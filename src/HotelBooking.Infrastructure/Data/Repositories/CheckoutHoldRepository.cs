using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Rooms;
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
            var pendingHolds = new List<(Guid HotelRoomTypeId, DateOnly CheckIn, DateOnly CheckOut, int Quantity)>();

            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.Add(holdDuration);


            foreach (var req in requests)
            {
                var totalRooms = await context.Rooms
                    .Where(r =>
                        r.HotelRoomTypeId == req.HotelRoomTypeId &&
                         r.Status == RoomStatus.Available &&
                        r.DeletedAtUtc == null)
                    .CountAsync(ct);

                var heldCount = await context.CheckoutHolds
                    .Where(h =>
                        h.HotelRoomTypeId == req.HotelRoomTypeId &&
                        !h.IsReleased &&
                        h.ExpiresAtUtc > now &&
                        h.CheckIn < req.CheckOut &&
                        h.CheckOut > req.CheckIn)
                    .SumAsync(h => (int?)h.Quantity ?? 0, ct);

                var bookedCount = await context.BookingRooms
                    .Where(br =>
                        br.HotelRoomTypeId == req.HotelRoomTypeId &&
                        br.Booking.Status != BookingStatus.Cancelled &&
                        br.Booking.Status != BookingStatus.Failed &&
                        br.Booking.CheckIn < req.CheckOut &&
                        br.Booking.CheckOut > req.CheckIn)
                    .CountAsync(ct);

                // NEW: count holds created earlier in this same loop
                var pendingHeldCount = pendingHolds
                    .Where(h =>
                        h.HotelRoomTypeId == req.HotelRoomTypeId &&
                        h.CheckIn < req.CheckOut &&
                        h.CheckOut > req.CheckIn)
                    .Sum(h => h.Quantity);

                var available = totalRooms - heldCount - bookedCount - pendingHeldCount;

                if (available < req.Quantity)
                {
                    await tx.RollbackAsync(ct);

                    var name = await context.HotelRoomTypes
                        .Where(rt => rt.Id == req.HotelRoomTypeId)
                        .Select(rt => rt.RoomType.Name)
                        .FirstOrDefaultAsync(ct);

                    return new HoldAcquisitionResult(
                                IsSuccess: false,
                                HoldIds: [],
                                ExpiresAtUtc: null,
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

                // IMPORTANT: reserve immediately in-memory for next iterations
                pendingHolds.Add((req.HotelRoomTypeId, req.CheckIn, req.CheckOut, req.Quantity));
            }

            await context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return new HoldAcquisitionResult(
                        IsSuccess: true,
                        HoldIds: createdHoldIds,
                        ExpiresAtUtc: expiresAt);
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