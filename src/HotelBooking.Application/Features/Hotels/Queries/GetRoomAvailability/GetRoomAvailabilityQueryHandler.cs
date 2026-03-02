using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Hotels;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Hotels.Queries.GetRoomAvailability;

public sealed class GetRoomAvailabilityQueryHandler(IAppDbContext context)
    : IRequestHandler<GetRoomAvailabilityQuery, Result<RoomAvailabilityResponse>>
{
    public async Task<Result<RoomAvailabilityResponse>> Handle(
        GetRoomAvailabilityQuery q, CancellationToken ct)
    {
        var hotel = await context.Hotels
            .AnyAsync(h => h.Id == q.HotelId, ct);

        if (!hotel)
            return HotelErrors.NotFound;

        var now = DateTimeOffset.UtcNow;

        var roomTypes = await context.HotelRoomTypes
            .Include(hrt => hrt.RoomType)
            .Include(hrt => hrt.Rooms)
            .Where(hrt => hrt.HotelId == q.HotelId && hrt.DeletedAtUtc == null)
            .ToListAsync(ct);

        var bookedCounts = await context.Bookings
            .Where(b => b.HotelId == q.HotelId
                     && b.Status != BookingStatus.Cancelled
                     && b.Status != BookingStatus.Failed
                     && b.CheckIn < q.CheckOut
                     && b.CheckOut > q.CheckIn)
            .SelectMany(b => b.BookingRooms)
            .GroupBy(br => br.HotelRoomTypeId)
            .Select(g => new { HotelRoomTypeId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // Held rooms (not expired + overlap)
        var heldCounts = await context.CheckoutHolds
            .AsNoTracking()
            .Where(ch => ch.HotelId == q.HotelId
                      && ch.IsReleased == false
                      && ch.ExpiresAtUtc > now
                      && ch.CheckIn < q.CheckOut
                      && ch.CheckOut > q.CheckIn)
            .GroupBy(ch => ch.HotelRoomTypeId)
            .Select(g => new { HotelRoomTypeId = g.Key, Count = g.Sum(x => x.Quantity) })
            .ToListAsync(ct);

        var availability = roomTypes.Select(hrt =>
        {
            var totalRooms = hrt.Rooms.Count(r => r.Status == "available");
            var booked = bookedCounts.FirstOrDefault(b => b.HotelRoomTypeId == hrt.Id)?.Count ?? 0;
            var held = heldCounts.FirstOrDefault(h => h.HotelRoomTypeId == hrt.Id)?.Count ?? 0;
            var available = Math.Max(0, totalRooms - booked - held);

            return new RoomAvailabilityDto(
                hrt.Id,
                hrt.RoomType.Name,
                hrt.PricePerNight,
                hrt.AdultCapacity,
                hrt.ChildCapacity,
                totalRooms,
                booked,
                held,
                available);
        }).ToList();

        return new RoomAvailabilityResponse(q.HotelId, q.CheckIn, q.CheckOut, availability);
    }
}