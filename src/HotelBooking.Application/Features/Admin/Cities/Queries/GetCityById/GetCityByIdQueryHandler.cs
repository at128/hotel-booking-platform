using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Admin;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Cities.Query.GetCityById;

public sealed class GetCityByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetCityByIdQuery, Result<CityDto>>
{
    public async Task<Result<CityDto>> Handle(GetCityByIdQuery q, CancellationToken ct)
    {
        var city = await db.Cities
            .AsNoTracking()
            .Where(c => c.Id == q.Id && c.DeletedAtUtc == null)
            .Select(c => new CityDto(
                c.Id,
                c.Name,
                c.Country,
                c.PostOffice,
                c.Hotels.Count(h => h.DeletedAtUtc == null),
                c.CreatedAtUtc,
                c.LastModifiedUtc))
            .FirstOrDefaultAsync(ct);

        if (city is null)
            return AdminErrors.Cities.NotFound;

        return city;
    }
}