using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Hotels.Query.GetHotelById;

public sealed class GetHotelByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetHotelByIdQuery, Result<HotelDto>>
{
    public async Task<Result<HotelDto>> Handle(GetHotelByIdQuery q, CancellationToken ct)
    {
        var hotel = await db.Hotels
            .AsNoTracking()
            .Where(h => h.Id == q.Id && h.DeletedAtUtc == null)
            .Select(h => new HotelDto(
                h.Id,
                h.CityId,
                h.City.Name,
                h.Name,
                h.Owner,
                h.Address,
                h.StarRating,
                h.Description,
                h.Latitude,
                h.Longitude,
                h.MinPricePerNight,
                h.AverageRating,
                h.ReviewCount,
                h.HotelRoomTypes.Count(rt => rt.DeletedAtUtc == null),
                h.CreatedAtUtc,
                h.LastModifiedUtc))
            .FirstOrDefaultAsync(ct);

        if (hotel is null)
            return AdminErrors.Hotels.NotFound;

        return hotel;
    }
}