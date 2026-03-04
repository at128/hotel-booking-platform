using HotelBooking.Application.Common.Errors;
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
        HandlePaymentWebhookCommand cmd, CancellationToken ct)
    {
        var evt = cmd.WebhookEvent;

        if (string.IsNullOrWhiteSpace(evt.ProviderSessionId) &&
    string.IsNullOrWhiteSpace(evt.TransactionRef))
        {
            logger.LogWarning(
                "Webhook event {EventType} has neither ProviderSessionId nor TransactionRef — skipping",
                evt.EventType);

            return Result.Updated;
        }

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

        if (payment is null)
        {
            logger.LogWarning(
                "No payment found for webhook event {EventType}. SessionId={SessionId}, TxRef={TxRef}",
                evt.EventType,
                evt.ProviderSessionId,
                evt.TransactionRef);

            return Result.Updated;
        }

        if (payment.Status is PaymentStatus.Succeeded or PaymentStatus.Failed)
        {
            logger.LogInformation(
                "Webhook {EventType} for session {SessionId} is a duplicate — already {Status}",
                evt.EventType, evt.ProviderSessionId, payment.Status);

            return Result.Updated;
        }

        string? logMessage = null;
        LogLevel? logLevel = null;

        try
        {
            switch (evt.EventType)
            {
                case PaymentEventTypes.PaymentSucceeded:
                    payment.MarkAsSucceeded(
                        transactionRef: evt.TransactionRef ?? evt.ProviderSessionId!,
                        responseJson: evt.RawPayload);

                    payment.Booking.Confirm();

                    logLevel = LogLevel.Information;
                    logMessage =
                        "Payment {PaymentId} succeeded for booking {BookingNumber}. TxRef={TxRef}";
                    break;

                case PaymentEventTypes.PaymentFailed:
                    payment.MarkAsFailed(responseJson: evt.RawPayload);
                    payment.Booking.MarkAsFailed();

                    logLevel = LogLevel.Warning;
                    logMessage =
                        "Payment {PaymentId} failed for booking {BookingNumber}";
                    break;

                default:
                    logger.LogDebug("Unhandled webhook event type: {EventType}", evt.EventType);
                    return Result.Updated; // Ack unknown events — don't retry
            }

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

            return Result.Updated; // Ack duplicate
        }

        if (logLevel == LogLevel.Information)
        {
            logger.LogInformation(
                logMessage!,
                payment.Id,
                payment.Booking.BookingNumber,
                evt.TransactionRef ?? evt.ProviderSessionId);
        }
        else if (logLevel == LogLevel.Warning)
        {
            logger.LogWarning(
                logMessage!,
                payment.Id,
                payment.Booking.BookingNumber);
        }

        if (evt.EventType == PaymentEventTypes.PaymentSucceeded)
        {
            if (string.IsNullOrWhiteSpace(payment.Booking.UserEmail))
            {
                logger.LogWarning(
                    "Payment succeeded for booking {BookingNumber}, but booking snapshot email is empty. Skipping confirmation email.",
                    payment.Booking.BookingNumber);
            }
            else
            {
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
        }

        return Result.Updated;
    }
}