using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Hotels;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Hotels.Enums;
using HotelBooking.Domain.Rooms;
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
            .AnyAsync(h => h.Id == q.HotelId && h.DeletedAtUtc == null, ct);

        if (!hotel)
            return HotelErrors.NotFound;

        var now = DateTimeOffset.UtcNow;

        var roomTypes = await context.HotelRoomTypes
            .AsNoTracking()
            .Where(hrt => hrt.HotelId == q.HotelId && hrt.DeletedAtUtc == null)
            .Select(hrt => new
            {
                hrt.Id,
                RoomTypeName = hrt.RoomType.Name,
                hrt.PricePerNight,
                hrt.AdultCapacity,
                hrt.ChildCapacity,
                hrt.MaxOccupancy,
                TotalRooms = hrt.Rooms.Count(r =>
                    r.Status == RoomStatus.Available &&
                    r.DeletedAtUtc == null)
            })
            .ToListAsync(ct);

        var roomTypeIds = roomTypes.Select(x => x.Id).ToList();

        var roomTypeImages = await context.Images
            .AsNoTracking()
            .Where(i => i.EntityType == ImageType.RoomType
                && roomTypeIds.Contains(i.EntityId))
            .OrderBy(i => i.SortOrder)
            .Select(i => new
            {
                i.EntityId,
                i.Url
            })
            .ToListAsync(ct);

        var imagesByType = roomTypeImages
            .GroupBy(x => x.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Url).ToList());

        var bookedCounts = await context.Bookings
            .AsNoTracking()
            .Where(b => b.HotelId == q.HotelId
                     && b.Status != BookingStatus.Cancelled
                     && b.Status != BookingStatus.Failed
                     && b.CheckIn < q.CheckOut
                     && b.CheckOut > q.CheckIn)
            .SelectMany(b => b.BookingRooms)
            .GroupBy(br => br.HotelRoomTypeId)
            .Select(g => new { HotelRoomTypeId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

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

        var bookedByType = bookedCounts.ToDictionary(x => x.HotelRoomTypeId, x => x.Count);
        var heldByType = heldCounts.ToDictionary(x => x.HotelRoomTypeId, x => x.Count);

        var availability = roomTypes.Select(hrt =>
        {
            var totalRooms = hrt.TotalRooms;
            var booked = bookedByType.GetValueOrDefault(hrt.Id, 0);
            var held = heldByType.GetValueOrDefault(hrt.Id, 0);
            var available = Math.Max(0, totalRooms - booked - held);
            var images = imagesByType.GetValueOrDefault(hrt.Id, new List<string>());

            return new RoomAvailabilityDto(
                hrt.Id,
                hrt.RoomTypeName,
                hrt.PricePerNight,
                hrt.AdultCapacity,
                hrt.ChildCapacity,
                hrt.MaxOccupancy,
                totalRooms,
                booked,
                held,
                available,
                images
            );
        }).ToList();

        return new RoomAvailabilityResponse(q.HotelId, q.CheckIn, q.CheckOut, availability);
    }
}