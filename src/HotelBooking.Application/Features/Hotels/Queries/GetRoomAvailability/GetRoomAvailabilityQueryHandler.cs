using HotelBooking.Application.Common.Availability;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Hotels;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Hotels.Enums;
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
                hrt.MaxOccupancy
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

        var countsByType = await RoomAvailabilityCalculator.GetCountsByRoomTypeAsync(
            context,
            roomTypeIds,
            q.CheckIn,
            q.CheckOut,
            now,
            ct);

        var availability = roomTypes.Select(hrt =>
        {
            var counts = countsByType.GetValueOrDefault(hrt.Id, RoomAvailabilityCounts.Empty);
            var available = Math.Max(0, counts.AvailableRooms);
            var images = imagesByType.GetValueOrDefault(hrt.Id, new List<string>());

            return new RoomAvailabilityDto(
                hrt.Id,
                hrt.RoomTypeName,
                hrt.PricePerNight,
                hrt.AdultCapacity,
                hrt.ChildCapacity,
                hrt.MaxOccupancy,
                counts.TotalRooms,
                counts.BookedRooms,
                counts.HeldRooms,
                available,
                images
            );
        }).ToList();

        return new RoomAvailabilityResponse(q.HotelId, q.CheckIn, q.CheckOut, availability);
    }
}
