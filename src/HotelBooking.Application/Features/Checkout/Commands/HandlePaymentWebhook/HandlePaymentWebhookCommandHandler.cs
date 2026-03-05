using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models.Payment;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Features.Checkout.Commands.HandlePaymentWebhook;

public sealed class HandlePaymentWebhookCommandHandler(
    IAppDbContext db,
    IEmailService emailService,
    ILogger<HandlePaymentWebhookCommandHandler> logger)
    : IRequestHandler<HandlePaymentWebhookCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(
        HandlePaymentWebhookCommand cmd,
        CancellationToken ct)
    {
        var evt = cmd.WebhookEvent;

        if (!HasUsableIdentifiers(evt))
        {
            logger.LogWarning(
                "Webhook event {EventType} has neither ProviderSessionId nor TransactionRef — skipping",
                evt.EventType);

            return Result.Updated;
        }

        var payment = await FindPaymentAsync(evt, ct);
        if (payment is null)
        {
            logger.LogWarning(
                "No payment found for webhook event {EventType}. SessionId={SessionId}, TxRef={TxRef}",
                evt.EventType,
                evt.ProviderSessionId,
                evt.TransactionRef);

            return Result.Updated;
        }

        var duplicateDecision = TryHandleDuplicateOrOutOfOrder(payment, evt);
        if (duplicateDecision.ShouldAckNow)
            return Result.Updated;

        var transition = ApplyWebhookTransition(payment, evt);
        if (transition.ShouldAckNow)
            return Result.Updated;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogInformation(
                ex,
                "Webhook event {EventType} caused an invalid state transition for payment {PaymentId} — treated as duplicate/out-of-order",
                evt.EventType,
                payment.Id);

            return Result.Updated;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogInformation(
                ex,
                "Concurrent webhook update detected for payment {PaymentId} (session {SessionId}) — treated as idempotent duplicate",
                payment.Id,
                evt.ProviderSessionId);

            return Result.Updated;
        }

        LogTransitionOutcome(payment, evt, transition);

        if (transition.ShouldSendConfirmationEmail)
        {
            await TrySendConfirmationEmailAsync(payment, evt, ct);
        }

        return Result.Updated;
    }

    private static bool HasUsableIdentifiers(WebhookParseResult evt)
        => !string.IsNullOrWhiteSpace(evt.ProviderSessionId)
           || !string.IsNullOrWhiteSpace(evt.TransactionRef);

    private async Task<HotelBooking.Domain.Bookings.Payment?> FindPaymentAsync(
        WebhookParseResult evt,
        CancellationToken ct)
    {
        var paymentQuery = db.Payments
            .Include(p => p.Booking)
            .AsQueryable();

        var payment = !string.IsNullOrWhiteSpace(evt.ProviderSessionId)
            ? await paymentQuery.FirstOrDefaultAsync(
                p => p.ProviderSessionId == evt.ProviderSessionId, ct)
            : null;

        if (payment is null && !string.IsNullOrWhiteSpace(evt.TransactionRef))
        {
            payment = await db.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.TransactionRef == evt.TransactionRef, ct);
        }

        return payment;
    }

    private DuplicateDecision TryHandleDuplicateOrOutOfOrder(
        HotelBooking.Domain.Bookings.Payment payment,
        WebhookParseResult evt)
    {
        var isSuccessWebhook = evt.EventType == PaymentEventTypes.PaymentSucceeded;
        var isFailedWebhook = evt.EventType == PaymentEventTypes.PaymentFailed;

        // Terminal success: always ignore later events
        if (payment.Status == PaymentStatus.Succeeded)
        {
            logger.LogInformation(
                "Webhook {EventType} for payment {PaymentId} is duplicate/out-of-order — already {Status}",
                evt.EventType,
                payment.Id,
                payment.Status);

            return DuplicateDecision.Ack();
        }

        // Failed + failed => duplicate
        if (payment.Status == PaymentStatus.Failed && isFailedWebhook)
        {
            logger.LogInformation(
                "Webhook {EventType} for payment {PaymentId} is duplicate — already {Status}",
                evt.EventType,
                payment.Id,
                payment.Status);

            return DuplicateDecision.Ack();
        }

        // Failed + success => possible recovery path
        if (payment.Status == PaymentStatus.Failed && isSuccessWebhook)
        {
            logger.LogWarning(
                "Late success webhook received for previously failed payment {PaymentId}. Recovery attempt will be validated.",
                payment.Id);
        }

        return DuplicateDecision.Continue();
    }

    private TransitionOutcome ApplyWebhookTransition(
        HotelBooking.Domain.Bookings.Payment payment,
        WebhookParseResult evt)
    {
        switch (evt.EventType)
        {
            case PaymentEventTypes.PaymentSucceeded:
                {
                    var txRef = evt.TransactionRef ?? evt.ProviderSessionId!;

                    if (payment.Status == PaymentStatus.Failed)
                    {
                        if (!payment.CanRecoverFromLocalTimeoutFailure())
                        {
                            logger.LogWarning(
                                "Ignoring late success webhook for payment {PaymentId}: failed state is not eligible for local-timeout recovery. SessionId={SessionId}",
                                payment.Id,
                                evt.ProviderSessionId);

                            return TransitionOutcome.AckWithoutStateChange();
                        }

                        payment.RecoverSucceededFromFailed(
                            transactionRef: txRef,
                            responseJson: evt.RawPayload);

                        payment.Booking.RecoverConfirmFromFailed();

                        return TransitionOutcome.SuccessRecovery();
                    }

                    payment.MarkAsSucceeded(
                        transactionRef: txRef,
                        responseJson: evt.RawPayload);

                    payment.Booking.Confirm();

                    return TransitionOutcome.SuccessNormal();
                }

            case PaymentEventTypes.PaymentFailed:
                {
                    payment.MarkAsFailed(responseJson: evt.RawPayload);
                    payment.Booking.MarkAsFailed();

                    return TransitionOutcome.FailureApplied();
                }

            default:
                logger.LogDebug(
                    "Unhandled webhook event type: {EventType}",
                    evt.EventType);

                return TransitionOutcome.AckWithoutStateChange();
        }
    }

    private void LogTransitionOutcome(
        HotelBooking.Domain.Bookings.Payment payment,
        WebhookParseResult evt,
        TransitionOutcome outcome)
    {
        if (outcome.Kind == TransitionKind.SuccessNormal)
        {
            logger.LogInformation(
                "Payment {PaymentId} succeeded for booking {BookingNumber}. TxRef={TxRef}",
                payment.Id,
                payment.Booking.BookingNumber,
                evt.TransactionRef ?? evt.ProviderSessionId);

            return;
        }

        if (outcome.Kind == TransitionKind.SuccessRecovered)
        {
            logger.LogWarning(
                "Payment {PaymentId} recovered to succeeded after local-timeout failure for booking {BookingNumber}. TxRef={TxRef}",
                payment.Id,
                payment.Booking.BookingNumber,
                evt.TransactionRef ?? evt.ProviderSessionId);

            return;
        }

        if (outcome.Kind == TransitionKind.FailureApplied)
        {
            logger.LogWarning(
                "Payment {PaymentId} failed for booking {BookingNumber}",
                payment.Id,
                payment.Booking.BookingNumber);
        }
    }

    private async Task TrySendConfirmationEmailAsync(
        HotelBooking.Domain.Bookings.Payment payment,
        WebhookParseResult evt,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(payment.Booking.UserEmail))
            {
                logger.LogWarning(
                    "Payment succeeded for booking {BookingNumber}, but booking snapshot email is empty. Skipping confirmation email.",
                    payment.Booking.BookingNumber);

                return;
            }

            var bookingRooms = await db.BookingRooms
                .AsNoTracking()
                .Where(br => br.BookingId == payment.BookingId)
                .Select(br => new BookingRoomEmailItem(
                    br.RoomTypeName,
                    br.RoomNumber,
                    br.PricePerNight))
                .ToListAsync(ct);

            var emailData = new BookingConfirmationEmailData(
                BookingNumber: payment.Booking.BookingNumber,
                HotelName: payment.Booking.HotelName,
                HotelAddress: payment.Booking.HotelAddress,
                CheckIn: payment.Booking.CheckIn,
                CheckOut: payment.Booking.CheckOut,
                Nights: payment.Booking.CheckOut.DayNumber - payment.Booking.CheckIn.DayNumber,
                TotalAmount: payment.Booking.TotalAmount,
                TransactionRef: evt.TransactionRef ?? evt.ProviderSessionId ?? string.Empty,
                Rooms: bookingRooms);

            await emailService.SendBookingConfirmationAsync(
                toEmail: payment.Booking.UserEmail,
                data: emailData,
                ct: ct);
        }
        catch (Exception ex)
        {
            // Don't fail webhook ACK after DB state has been committed
            logger.LogError(
                ex,
                "Failed to send booking confirmation email after successful payment. BookingNumber={BookingNumber}, PaymentId={PaymentId}",
                payment.Booking.BookingNumber,
                payment.Id);
        }
    }

    private readonly record struct DuplicateDecision(bool ShouldAckNow)
    {
        public static DuplicateDecision Ack() => new(true);
        public static DuplicateDecision Continue() => new(false);
    }

    private readonly record struct TransitionOutcome(
        TransitionKind Kind,
        bool ShouldAckNow = false,
        bool ShouldSendConfirmationEmail = false)
    {
        public static TransitionOutcome AckWithoutStateChange()
            => new(TransitionKind.None, ShouldAckNow: true, ShouldSendConfirmationEmail: false);

        public static TransitionOutcome SuccessNormal()
            => new(TransitionKind.SuccessNormal, ShouldAckNow: false, ShouldSendConfirmationEmail: true);

        public static TransitionOutcome SuccessRecovery()
            => new(TransitionKind.SuccessRecovered, ShouldAckNow: false, ShouldSendConfirmationEmail: true);

        public static TransitionOutcome FailureApplied()
            => new(TransitionKind.FailureApplied, ShouldAckNow: false, ShouldSendConfirmationEmail: false);
    }

    private enum TransitionKind
    {
        None = 0,
        SuccessNormal = 1,
        SuccessRecovered = 2,
        FailureApplied = 3
    }
}