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
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Country,
                HotelCount = context.Hotels.Count(h => h.CityId == c.Id && h.DeletedAtUtc == null),
                VisitCount = context.HotelVisits
                    .Where(hv => hv.VisitedAtUtc >= thirtyDaysAgo && hv.Hotel.CityId == c.Id)
                    .Count(),
                ThumbnailUrl = context.Hotels
                    .Where(h => h.CityId == c.Id && h.DeletedAtUtc == null && h.ThumbnailUrl != null)
                    .OrderBy(h => h.Name)
                    .Select(h => h.ThumbnailUrl)
                    .FirstOrDefault()
            })
            .OrderByDescending(c => c.VisitCount)
            .ThenByDescending(c => c.HotelCount)
            .Take(5)
            .Select(c => new TrendingCityDto(
                c.Id,
                c.Name,
                c.Country,
                c.HotelCount,
                c.VisitCount,
                c.ThumbnailUrl))
            .ToListAsync(ct);

        return new TrendingCitiesResponse(cities);
    }
}
