using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models.Payment;
using HotelBooking.Application.Settings;
using HotelBooking.Contracts.Checkout;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelBooking.Application.Features.Checkout.Commands.CancelBooking;

public sealed class CancelBookingCommandHandler(
    IAppDbContext db,
    IPaymentGateway paymentGateway,
    IOptions<BookingSettings> bookingOptions,
    ILogger<CancelBookingCommandHandler> logger)
    : IRequestHandler<CancelBookingCommand, Result<CancellationDetailsResponse>>
{
    private readonly BookingSettings _settings = bookingOptions.Value;

    public async Task<Result<CancellationDetailsResponse>> Handle(
        CancelBookingCommand cmd,
        CancellationToken ct)
    {
        var booking = await LoadBookingAsync(cmd.BookingId, ct);
        if (booking is null)
            return ApplicationErrors.Booking.NotFound;

        if (!HasAccess(booking, cmd))
            return ApplicationErrors.Booking.AccessDenied;

        var cancellationContextResult = await EnsureCancellationAsync(booking, cmd, ct);
        if (cancellationContextResult.IsError)
            return cancellationContextResult.TopError;

        var context = cancellationContextResult.Value;

        await ProcessRefundIfPendingAsync(context.Booking, context.Cancellation, ct);

        return MapCancellationResponse(context.Booking, context.Cancellation);
    }

    private async Task<Booking?> LoadBookingAsync(Guid bookingId, CancellationToken ct)
    {
        return await db.Bookings
            .Include(b => b.Cancellation)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct);
    }

    private static bool HasAccess(Booking booking, CancelBookingCommand cmd)
        => cmd.IsAdmin || booking.UserId == cmd.RequestingUserId;

    /// <summary>
    /// Ensures a cancellation record exists.
    /// - If booking is already cancelled and cancellation exists: reuse it (idempotent retry path).
    /// - Otherwise, create a new cancellation transactionally.
    /// </summary>
    private async Task<Result<CancellationContext>> EnsureCancellationAsync(
        Booking booking,
        CancelBookingCommand cmd,
        CancellationToken ct)
    {
        if (booking.Status == BookingStatus.Cancelled)
        {
            if (booking.Cancellation is null)
            {
                return Error.Failure(
                    "Booking.CancellationStateInvalid",
                    "Booking is cancelled but cancellation record is missing.");
            }

            return new CancellationContext(booking, booking.Cancellation);
        }

        var quoteResult = BuildCancellationQuote(booking);
        if (quoteResult.IsError)
            return quoteResult.TopError;

        var quote = quoteResult.Value;

        await using (var tx = await db.BeginTransactionAsync(ct))
        {
            booking.Cancel();

            var cancellation = new Cancellation(
                id: Guid.CreateVersion7(),
                bookingId: booking.Id,
                reason: NormalizeReason(cmd.Reason),
                refundAmount: quote.RefundAmount,
                refundPercentage: quote.RefundPercentage);

            db.Cancellations.Add(cancellation);

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        var reloadedBooking = await LoadBookingAsync(booking.Id, ct);
        if (reloadedBooking is null || reloadedBooking.Cancellation is null)
        {
            return Error.Failure(
                "Booking.CancellationReloadFailed",
                "Booking cancellation was created but failed to reload.");
        }

        return new CancellationContext(reloadedBooking, reloadedBooking.Cancellation);
    }

    private Result<CancellationQuote> BuildCancellationQuote(Booking booking)
    {
        if (booking.Status != BookingStatus.Confirmed)
        {
            return Error.Conflict(
                "Booking.CannotCancel",
                $"Cannot cancel a booking in '{booking.Status}' status.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (booking.CheckIn <= today)
        {
            return Error.Conflict(
                "Booking.CheckedIn",
                "Cannot cancel a booking on or after the check-in date.");
        }

        // NOTE: This assumes LastModifiedUtc approximates confirmation time in current model.
        var confirmedAt = booking.LastModifiedUtc;
        var cancelledAt = DateTimeOffset.UtcNow;
        var hoursSinceConfirmation = (cancelledAt - confirmedAt).TotalHours;

        var freeWindowHours = Math.Max(0, _settings.CancellationFreeHours);
        var feePercent = Math.Clamp(_settings.CancellationFeePercent, 0m, 1m);

        var isFreeCancellation = hoursSinceConfirmation <= freeWindowHours;
        var refundPercentage = isFreeCancellation ? 1.0m : (1.0m - feePercent);
        refundPercentage = Math.Clamp(refundPercentage, 0m, 1m);

        var refundAmount = Math.Round(
            booking.TotalAmount * refundPercentage,
            2,
            MidpointRounding.AwayFromZero);

        return new CancellationQuote(refundAmount, refundPercentage);
    }

    /// <summary>
    /// Idempotent refund processor:
    /// - If refund already terminal => no-op
    /// - If amount = 0 => mark processed
    /// - If gateway fails/transient/throws => keep Pending (minimal-safe behavior)
    /// - Uses stable idempotency key per cancellation
    /// </summary>
    private async Task ProcessRefundIfPendingAsync(
        Booking booking,
        Cancellation cancellation,
        CancellationToken ct)
    {
        // Terminal state => idempotent no-op
        if (cancellation.RefundStatus != RefundStatus.Pending)
            return;

        if (TryProcessZeroAmountRefund(cancellation))
        {
            await db.SaveChangesAsync(ct);
            return;
        }

        var refundablePayment = FindLatestSucceededRefundablePayment(booking);
        if (refundablePayment is null)
        {
            // This is a local invariant/data issue (not a transient gateway failure).
            // We can mark failed because there is no successful payment to refund.
            logger.LogWarning(
                "No refundable succeeded payment found for cancelled booking {BookingId}. Marking refund as failed. CancellationId={CancellationId}",
                booking.Id,
                cancellation.Id);

            cancellation.MarkRefundFailed();
            await db.SaveChangesAsync(ct);
            return;
        }

        var refundResult = await TryExecuteRefundAsync(
            refundablePayment,
            cancellation,
            ct);

        // Exception or provider non-success => keep Pending for retry (minimal-safe behavior)
        if (refundResult is null)
            return;

        if (!refundResult.IsSuccess)
        {
            logger.LogWarning(
                "Refund provider returned non-success for booking {BookingId}, cancellation {CancellationId}. Keeping refund pending. Error={Error}",
                booking.Id,
                cancellation.Id,
                refundResult.ErrorMessage);

            return;
        }

        ApplySuccessfulRefund(refundablePayment, cancellation);
        await db.SaveChangesAsync(ct);
    }

    private static bool TryProcessZeroAmountRefund(Cancellation cancellation)
    {
        if (cancellation.RefundAmount > 0)
            return false;

        cancellation.MarkRefundProcessed();
        return true;
    }

    private static Payment? FindLatestSucceededRefundablePayment(Booking booking)
    {
        return booking.Payments
            .Where(p => p.Status == PaymentStatus.Succeeded)
            .OrderByDescending(p => p.PaidAtUtc ?? p.CreatedAtUtc)
            .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.TransactionRef));
    }

    /// <summary>
    /// Returns:
    /// - RefundResponse => gateway responded
    /// - null => exception happened; kept pending intentionally
    /// </summary>
    private async Task<RefundResponse?> TryExecuteRefundAsync(
        Payment succeededPayment,
        Cancellation cancellation,
        CancellationToken ct)
    {
        var idempotencyKey = $"refund:{cancellation.Id}";

        try
        {
            return await paymentGateway.RefundAsync(
                transactionRef: succeededPayment.TransactionRef!,
                amount: cancellation.RefundAmount,
                idempotencyKey: idempotencyKey,
                ct: ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Minimal-safe behavior:
            // Do not mark refund failed on exceptions (timeout/network/provider issue).
            // Keep Pending and allow caller/job/manual retry.
            logger.LogWarning(
                ex,
                "Refund gateway exception for cancellation {CancellationId} (payment {PaymentId}). Keeping refund pending for retry.",
                cancellation.Id,
                succeededPayment.Id);

            return null;
        }
    }

    private static void ApplySuccessfulRefund(
        Payment succeededPayment,
        Cancellation cancellation)
    {
        cancellation.MarkRefundProcessed();

        if (cancellation.RefundAmount >= succeededPayment.Amount)
            succeededPayment.MarkAsRefunded();
        else
            succeededPayment.MarkAsPartiallyRefunded();
    }

    private static string? NormalizeReason(string? reason)
        => string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

    private static CancellationDetailsResponse MapCancellationResponse(
        Booking booking,
        Cancellation cancellation)
    {
        return new CancellationDetailsResponse(
            BookingId: booking.Id,
            BookingNumber: booking.BookingNumber,
            RefundAmount: cancellation.RefundAmount,
            RefundPercentage: cancellation.RefundPercentage,
            RefundStatus: cancellation.RefundStatus.ToString(),
            CancelledAtUtc: cancellation.CancelledAtUtc,
            Reason: cancellation.Reason);
    }

    private readonly record struct CancellationQuote(decimal RefundAmount, decimal RefundPercentage);

    private sealed record CancellationContext(Booking Booking, Cancellation Cancellation);
}