using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Settings;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelBooking.Application.Features.Checkout.Commands.ExpirePendingPayments;

public sealed class ExpirePendingPaymentsCommandHandler(
    IAppDbContext db,
    IOptions<BookingSettings> bookingOptions,
    ILogger<ExpirePendingPaymentsCommandHandler> logger)
    : IRequestHandler<ExpirePendingPaymentsCommand, Result<Updated>>
{
    private readonly BookingSettings _booking = bookingOptions.Value;

    public async Task<Result<Updated>> Handle(
        ExpirePendingPaymentsCommand cmd,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var cutoff = now.AddMinutes(-_booking.CheckoutHoldMinutes);
        var batchSize = Math.Clamp(cmd.BatchSize, 1, 500);

        // Fetch IDs only for performance
        var candidatePaymentIds = await db.Payments
            .AsNoTracking()
            .Where(p =>
                (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.InitiationFailed) &&
                p.CreatedAtUtc <= cutoff)
            .OrderBy(p => p.CreatedAtUtc)
            .Select(p => p.Id)
            .Take(batchSize)
            .ToListAsync(ct);

        if (candidatePaymentIds.Count == 0)
        {
            logger.LogDebug("ExpirePendingPaymentsJob: no expired pending payments found.");
            return Result.Updated;
        }

        var expiredCount = 0;
        var skippedCount = 0;

        try
        {
            var batchResult = await ExpireBatchAsync(candidatePaymentIds, cutoff, ct);
            expiredCount += batchResult.Expired;
            skippedCount += batchResult.Skipped;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Fallback to per-item for robustness under heavy concurrency (e.g., webhook racing)
            logger.LogInformation(
                ex,
                "ExpirePendingPaymentsJob: batch concurrency conflict. Falling back to per-item processing for {Count} payments.",
                candidatePaymentIds.Count);

            db.ClearChangeTracker();

            foreach (var paymentId in candidatePaymentIds)
            {
                try
                {
                    var changed = await ExpireOneAsync(paymentId, cutoff, ct);
                    if (changed)
                        expiredCount++;
                    else
                        skippedCount++;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception itemEx)
                {
                    skippedCount++;
                    logger.LogError(itemEx,
                        "ExpirePendingPaymentsJob: fallback processing failed for payment {PaymentId}",
                        paymentId);
                }
            }
        }

        logger.LogInformation(
            "ExpirePendingPaymentsJob completed. Candidates={Candidates}, Expired={Expired}, Skipped={Skipped}",
            candidatePaymentIds.Count, expiredCount, skippedCount);

        return Result.Updated;
    }

    private async Task<BatchExpireResult> ExpireBatchAsync(
        List<Guid> paymentIds,
        DateTimeOffset cutoff,
        CancellationToken ct)
    {
        await using var tx = await db.BeginTransactionAsync(ct);

        var payments = await db.Payments
            .Include(p => p.Booking)
            .Where(p => paymentIds.Contains(p.Id))
            .ToListAsync(ct);

        var expired = 0;
        var skipped = 0;

        foreach (var payment in payments)
        {
            // Re-check inside transaction (important for races with webhook)
            if (payment.CreatedAtUtc > cutoff)
            {
                skipped++;
                continue;
            }

            if (payment.Status is not (PaymentStatus.Pending or PaymentStatus.InitiationFailed))
            {
                skipped++;
                continue;
            }

            try
            {
                if (payment.Status == PaymentStatus.Pending)
                {
                    payment.MarkAsFailed("{\"reason\":\"payment_timeout\"}");
                }
                else
                {
                    // Requires MarkAsFailed to allow transition from InitiationFailed (recommended)
                    payment.MarkAsFailed("{\"reason\":\"payment_initiation_timeout\"}");
                }

                // Using existing domain behavior for compatibility with current codebase.
                // Later you can introduce MarkAsExpired() for better semantics.
                payment.Booking.MarkAsFailed();

                expired++;
            }
            catch (InvalidOperationException ex)
            {
                await RevertTrackedPaymentAndBookingAsync(payment, ct);
                // Likely processed by webhook concurrently / out-of-order state transition
                skipped++;

                logger.LogInformation(
                    ex,
                    "ExpirePendingPaymentsJob batch skip due to invalid state transition for payment {PaymentId}",
                    payment.Id);
            }
        }

        if (expired == 0)
        {
            await tx.RollbackAsync(ct);
            return new BatchExpireResult(Expired: 0, Skipped: skipped);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new BatchExpireResult(Expired: expired, Skipped: skipped);
    }

    private async Task<bool> ExpireOneAsync(
        Guid paymentId,
        DateTimeOffset cutoff,
        CancellationToken ct)
    {
        await using var tx = await db.BeginTransactionAsync(ct);

        try
        {
            var payment = await db.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.Id == paymentId, ct);

            if (payment is null)
            {
                await tx.RollbackAsync(ct);
                return false;
            }

            // Re-check inside transaction
            if (payment.CreatedAtUtc > cutoff)
            {
                await tx.RollbackAsync(ct);
                return false;
            }

            if (payment.Status is not (PaymentStatus.Pending or PaymentStatus.InitiationFailed))
            {
                await tx.RollbackAsync(ct);
                return false;
            }

            if (payment.Status == PaymentStatus.Pending)
            {
                payment.MarkAsFailed("{\"reason\":\"payment_timeout\"}");
            }
            else
            {
                payment.MarkAsFailed("{\"reason\":\"payment_initiation_timeout\"}");
            }

            payment.Booking.MarkAsFailed();

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogInformation(
                ex,
                "ExpirePendingPaymentsJob fallback: concurrency conflict for payment {PaymentId} — treated as already processed",
                paymentId);

            try { await tx.RollbackAsync(ct); } catch { /* no-op */ }
            db.ClearChangeTracker();
            return false;
        }
        catch (InvalidOperationException ex)
        {
            logger.LogInformation(
                ex,
                "ExpirePendingPaymentsJob fallback: invalid state transition for payment {PaymentId} — treated as already processed/out-of-order",
                paymentId);

            try { await tx.RollbackAsync(ct); } catch { /* no-op */ }
            db.ClearChangeTracker();
            return false;
        }
        catch (OperationCanceledException)
        {
            try { await tx.RollbackAsync(CancellationToken.None); } catch { /* no-op */ }
            throw;
        }
        catch
        {
            try { await tx.RollbackAsync(ct); } catch { /* no-op */ }
            throw;
        }
    }


    private async Task RevertTrackedPaymentAndBookingAsync(
    HotelBooking.Domain.Bookings.Payment payment,
    CancellationToken ct)
    {
        await db.ReloadEntityAsync(payment, ct);
        await db.ReloadEntityAsync(payment.Booking, ct);
    }

    private sealed record BatchExpireResult(int Expired, int Skipped);
}