using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Bookings;
using HotelBooking.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace HotelBooking.Infrastructure.Data.Repositories;

public sealed class CheckoutHoldRepository(AppDbContext context)
    : ICheckoutHoldRepository
{
    public async Task<HoldAcquisitionResult> TryAcquireHoldsAsync(
        Guid userId,
        List<HoldRequest> requests,
        TimeSpan holdDuration,
        CancellationToken ct = default)
    {
        // SERIALIZABLE isolation: prevents phantom reads
        // Two concurrent transactions cannot both see "2 rooms available" and both insert holds
        await using IDbContextTransaction tx = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var createdHoldIds = new List<Guid>();
            var expiresAt = DateTimeOffset.UtcNow.Add(holdDuration);

            foreach (var req in requests)
            {
                // Count total rooms of this type
                var totalRooms = await context.Rooms
                    .Where(r =>
                        r.HotelRoomTypeId == req.HotelRoomTypeId &&
                        r.DeletedAtUtc == null)
                    .CountAsync(ct);

                // Count active holds for this room type and date range
                var heldCount = await context.CheckoutHolds
                    .Where(h =>
                        h.HotelRoomTypeId == req.HotelRoomTypeId &&
                        !h.IsReleased &&
                        h.ExpiresAtUtc > DateTimeOffset.UtcNow &&
                        h.CheckIn < req.CheckOut &&
                        h.CheckOut > req.CheckIn)
                    .SumAsync(h => (int?)h.Quantity ?? 0, ct);

                // Count confirmed bookings for overlapping dates
                var bookedCount = await context.BookingRooms
                    .Include(br => br.Booking)
                    .Where(br =>
                        br.HotelRoomTypeId == req.HotelRoomTypeId &&
                        br.Booking.Status != Domain.Bookings.Enums.BookingStatus.Cancelled &&
                        br.Booking.Status != Domain.Bookings.Enums.BookingStatus.Failed &&
                        br.Booking.CheckIn < req.CheckOut &&
                        br.Booking.CheckOut > req.CheckIn)
                    .CountAsync(ct);

                var available = totalRooms - heldCount - bookedCount;

                if (available < req.Quantity)
                {
                    await tx.RollbackAsync(ct);

                    // Get room type name for user-friendly error
                    var name = await context.HotelRoomTypes
                        .Where(rt => rt.Id == req.HotelRoomTypeId)
                        .Select(rt => rt.RoomType.Name)
                        .FirstOrDefaultAsync(ct);

                    return new HoldAcquisitionResult(
                        IsSuccess: false,
                        HoldIds: [],
                        FailedRoomTypeName: name ?? "Unknown");
                }

                var hold = new CheckoutHold(
                    id: Guid.CreateVersion7(),
                    userId: userId,
                    hotelId: req.HotelId,
                    hotelRoomTypeId: req.HotelRoomTypeId,
                    checkIn: req.CheckIn,
                    checkOut: req.CheckOut,
                    quantity: req.Quantity,
                    expiresAtUtc: expiresAt);

                context.CheckoutHolds.Add(hold);
                createdHoldIds.Add(hold.Id);
            }

            await context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return new HoldAcquisitionResult(
                IsSuccess: true,
                HoldIds: createdHoldIds);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task ReleaseHoldsAsync(
        Guid userId, CancellationToken ct = default)
    {
        await context.CheckoutHolds
            .Where(h => h.UserId == userId && !h.IsReleased)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(h => h.IsReleased, true), ct);
    }

    public async Task<List<ActiveHoldDto>> GetActiveHoldsAsync(
        Guid userId, CancellationToken ct = default)
    {
        return await context.CheckoutHolds
            .AsNoTracking()
            .Include(h => h.HotelRoomType)
            .Where(h =>
                h.UserId == userId &&
                !h.IsReleased &&
                h.ExpiresAtUtc > DateTimeOffset.UtcNow)
            .Select(h => new ActiveHoldDto(
                h.Id,
                h.HotelRoomTypeId,
                h.HotelRoomType.RoomType.Name,
                h.Quantity,
                h.ExpiresAtUtc))
            .ToListAsync(ct);
    }
}