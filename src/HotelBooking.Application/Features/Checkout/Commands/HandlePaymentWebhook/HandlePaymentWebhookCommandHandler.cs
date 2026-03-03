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
    ILogger<HandlePaymentWebhookCommandHandler> logger)
    : IRequestHandler<HandlePaymentWebhookCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(
        HandlePaymentWebhookCommand cmd, CancellationToken ct)
    {
        var evt = cmd.WebhookEvent;

        if (string.IsNullOrEmpty(evt.ProviderSessionId))
        {
            logger.LogWarning(
                "Webhook event {EventType} has no ProviderSessionId — skipping",
                evt.EventType);

            return Result.Updated;
        }

        var payment = await db.Payments
            .Include(p => p.Booking)
            .FirstOrDefaultAsync(p => p.ProviderSessionId == evt.ProviderSessionId, ct);

        if (payment is null)
        {
            logger.LogWarning(
                "No payment found for ProviderSessionId {SessionId} (event: {EventType})",
                evt.ProviderSessionId, evt.EventType);

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

        switch (evt.EventType)
        {
            case PaymentEventTypes.PaymentSucceeded:
                payment.MarkAsSucceeded(
                    transactionRef: evt.TransactionRef ?? evt.ProviderSessionId,
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

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogInformation(
                ex,
                "Concurrent webhook update detected for payment {PaymentId} (session {SessionId}) — treated as idempotent duplicate",
                payment.Id, evt.ProviderSessionId);

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

        return Result.Updated;
    }
}