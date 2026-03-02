using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Hotels;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Hotels.Queries.GetHotelDetails;

public sealed class GetHotelDetailsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetHotelDetailsQuery, Result<HotelDetailsDto>>
{
    public async Task<Result<HotelDetailsDto>> Handle(
        GetHotelDetailsQuery query, CancellationToken ct)
    {
        var hotel = await context.Hotels
            .Include(h => h.City)
            .Include(h => h.HotelServices).ThenInclude(hs => hs.Service)
            .Include(h => h.HotelRoomTypes).ThenInclude(hrt => hrt.RoomType)
            .FirstOrDefaultAsync(h => h.Id == query.HotelId, ct);

        if (hotel is null)
            return HotelErrors.NotFound;

        return new HotelDetailsDto(
            hotel.Id,
            hotel.Name,
            hotel.Description,
            hotel.StarRating,
            hotel.Owner,
            hotel.Address,
            hotel.Latitude,
            hotel.Longitude,
            hotel.CheckInTime,
            hotel.CheckOutTime,
            hotel.City.Name,
            hotel.City.Country,
            hotel.AverageRating,
            hotel.ReviewCount,
            hotel.ThumbnailUrl,
            hotel.HotelServices.Select(hs => hs.Service.Name).ToList(),
            hotel.HotelRoomTypes
                .Where(hrt => hrt.DeletedAtUtc == null)
                .Select(hrt => new HotelRoomTypeDto(
                    hrt.Id,
                    hrt.RoomType.Name,
                    hrt.Description,
                    hrt.PricePerNight,
                    hrt.AdultCapacity,
                    hrt.ChildCapacity))
                .ToList());
    }
}