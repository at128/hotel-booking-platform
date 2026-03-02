using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Home;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Home.Queries.GetTrendingCities;

public sealed class GetTrendingCitiesQueryHandler(IAppDbContext context)
    : IRequestHandler<GetTrendingCitiesQuery, Result<TrendingCitiesResponse>>
{
    public async Task<Result<TrendingCitiesResponse>> Handle(
        GetTrendingCitiesQuery request, CancellationToken ct)
    {
        var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);

        var cities = await context.Cities
            .Select(c => new TrendingCityDto(
                c.Id,
                c.Name,
                c.Country,
                c.Hotels.Count,
                context.HotelVisits
                    .Count(hv => hv.Hotel.CityId == c.Id && hv.VisitedAtUtc >= thirtyDaysAgo)))
            .OrderByDescending(c => c.VisitCount)
            .ThenByDescending(c => c.HotelCount)
            .Take(5)
            .ToListAsync(ct);

        return new TrendingCitiesResponse(cities);
    }
}