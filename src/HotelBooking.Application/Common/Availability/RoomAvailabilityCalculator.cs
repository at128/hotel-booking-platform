using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Rooms;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Common.Availability;

public sealed record RoomAvailabilityCounts(
    int TotalRooms,
    int BookedRooms,
    int HeldRooms)
{
    public int AvailableRooms => TotalRooms - BookedRooms - HeldRooms;

    public static readonly RoomAvailabilityCounts Empty = new(0, 0, 0);
}

public static class RoomAvailabilityCalculator
{
    public static async Task<RoomAvailabilityCounts> GetCountsAsync(
        IAppDbContext db,
        Guid hotelRoomTypeId,
        DateOnly checkIn,
        DateOnly checkOut,
        DateTimeOffset nowUtc,
        CancellationToken ct = default)
    {
        var countsByType = await GetCountsByRoomTypeAsync(
            db,
            [hotelRoomTypeId],
            checkIn,
            checkOut,
            nowUtc,
            ct);

        return countsByType.GetValueOrDefault(hotelRoomTypeId, RoomAvailabilityCounts.Empty);
    }

    public static async Task<IReadOnlyDictionary<Guid, RoomAvailabilityCounts>> GetCountsByRoomTypeAsync(
        IAppDbContext db,
        IReadOnlyCollection<Guid> hotelRoomTypeIds,
        DateOnly checkIn,
        DateOnly checkOut,
        DateTimeOffset nowUtc,
        CancellationToken ct = default)
    {
        if (hotelRoomTypeIds.Count == 0)
            return new Dictionary<Guid, RoomAvailabilityCounts>();

        var ids = hotelRoomTypeIds.Distinct().ToList();

        var totalRoomsByType = await db.Rooms
            .AsNoTracking()
            .Where(r =>
                ids.Contains(r.HotelRoomTypeId) &&
                r.DeletedAtUtc == null &&
                r.Status == RoomStatus.Available)
            .GroupBy(r => r.HotelRoomTypeId)
            .Select(g => new
            {
                HotelRoomTypeId = g.Key,
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.HotelRoomTypeId, x => x.Count, ct);

        var bookedRoomsByType = await db.BookingRooms
            .AsNoTracking()
            .Where(br =>
                ids.Contains(br.HotelRoomTypeId) &&
                br.Booking.Status != BookingStatus.Cancelled &&
                br.Booking.Status != BookingStatus.Failed &&
                br.Booking.CheckIn < checkOut &&
                br.Booking.CheckOut > checkIn)
            .GroupBy(br => br.HotelRoomTypeId)
            .Select(g => new
            {
                HotelRoomTypeId = g.Key,
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.HotelRoomTypeId, x => x.Count, ct);

        var heldRoomsByType = await db.CheckoutHolds
            .AsNoTracking()
            .Where(ch =>
                ids.Contains(ch.HotelRoomTypeId) &&
                !ch.IsReleased &&
                ch.ExpiresAtUtc > nowUtc &&
                ch.CheckIn < checkOut &&
                ch.CheckOut > checkIn)
            .GroupBy(ch => ch.HotelRoomTypeId)
            .Select(g => new
            {
                HotelRoomTypeId = g.Key,
                Count = g.Sum(x => x.Quantity)
            })
            .ToDictionaryAsync(x => x.HotelRoomTypeId, x => x.Count, ct);

        var result = new Dictionary<Guid, RoomAvailabilityCounts>(ids.Count);

        foreach (var id in ids)
        {
            var total = totalRoomsByType.GetValueOrDefault(id, 0);
            var booked = bookedRoomsByType.GetValueOrDefault(id, 0);
            var held = heldRoomsByType.GetValueOrDefault(id, 0);

            result[id] = new RoomAvailabilityCounts(total, booked, held);
        }

        return result;
    }
}
