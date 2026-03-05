using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Settings;
using HotelBooking.Contracts.Checkout;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HotelBooking.Application.Features.Checkout.Commands.CancelBooking;

public sealed class CancelBookingCommandHandler(
    IAppDbContext db,
    IPaymentGateway paymentGateway,
    IOptions<BookingSettings> bookingOptions)
    : IRequestHandler<CancelBookingCommand, Result<CancellationDetailsResponse>>
{
    private readonly BookingSettings _settings = bookingOptions.Value;

    public async Task<Result<CancellationDetailsResponse>> Handle(
        CancelBookingCommand cmd,
        CancellationToken ct)
    {
        var booking = await db.Bookings
            .Include(b => b.Cancellation)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == cmd.BookingId, ct);

        if (booking is null)
            return ApplicationErrors.Booking.NotFound;

        if (!cmd.IsAdmin && booking.UserId != cmd.RequestingUserId)
            return ApplicationErrors.Booking.AccessDenied;

        if (booking.Status == BookingStatus.Cancelled && booking.Cancellation is not null)
            return MapCancellationResponse(booking, booking.Cancellation);

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

        var succeededPayment = booking.Payments
            .Where(p => p.Status == PaymentStatus.Succeeded)
            .OrderByDescending(p => p.PaidAtUtc ?? p.CreatedAtUtc)
            .FirstOrDefault();

        await using (var tx = await db.BeginTransactionAsync(ct))
        {
            booking.Cancel();

            var cancellation = new Cancellation(
                id: Guid.CreateVersion7(),
                bookingId: booking.Id,
                reason: string.IsNullOrWhiteSpace(cmd.Reason) ? null : cmd.Reason.Trim(),
                refundAmount: refundAmount,
                refundPercentage: refundPercentage);

            db.Cancellations.Add(cancellation);

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        booking = await db.Bookings
            .Include(b => b.Cancellation)
            .Include(b => b.Payments)
            .FirstAsync(b => b.Id == cmd.BookingId, ct);

        var createdCancellation = booking.Cancellation!;

        if (refundAmount <= 0)
        {
            if (createdCancellation.RefundStatus == RefundStatus.Pending)
            {
                createdCancellation.MarkRefundProcessed();
                await db.SaveChangesAsync(ct);
            }

            return MapCancellationResponse(booking, createdCancellation);
        }

        if (succeededPayment is null || string.IsNullOrWhiteSpace(succeededPayment.TransactionRef))
        {
            if (createdCancellation.RefundStatus == RefundStatus.Pending)
            {
                createdCancellation.MarkRefundFailed();
                await db.SaveChangesAsync(ct);
            }

            return MapCancellationResponse(booking, createdCancellation);
        }

        var idempotencyKey = $"refund:{booking.Id}";

        var refundResult = await paymentGateway.RefundAsync(
            transactionRef: succeededPayment.TransactionRef,
            amount: refundAmount,
            idempotencyKey: idempotencyKey,
            ct: ct);

        if (createdCancellation.RefundStatus == RefundStatus.Pending)
        {
            if (refundResult.IsSuccess)
            {
                createdCancellation.MarkRefundProcessed();

                if (refundAmount >= succeededPayment.Amount)
                    succeededPayment.MarkAsRefunded();
                else
                    succeededPayment.MarkAsPartiallyRefunded();
            }
            else
            {
                createdCancellation.MarkRefundFailed();
            }

            await db.SaveChangesAsync(ct);
        }

        return MapCancellationResponse(booking, createdCancellation);
    }

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
}