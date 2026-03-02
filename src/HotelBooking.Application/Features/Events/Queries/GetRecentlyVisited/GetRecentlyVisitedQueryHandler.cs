using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Events;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Events.Queries.GetRecentlyVisited;

public sealed class GetRecentlyVisitedQueryHandler(IAppDbContext context)
    : IRequestHandler<GetRecentlyVisitedQuery, Result<RecentlyVisitedResponse>>
{
    public async Task<Result<RecentlyVisitedResponse>> Handle(
        GetRecentlyVisitedQuery query, CancellationToken ct)
    {
        var recentVisits = await context.HotelVisits
            .Where(hv => hv.UserId == query.UserId)
            .GroupBy(hv => hv.HotelId)
            .Select(g => new
            {
                HotelId = g.Key,
                LatestVisit = g.Max(x => x.VisitedAtUtc)
            })
            .OrderByDescending(x => x.LatestVisit)
            .Take(5)
            .ToListAsync(ct);

        if (recentVisits.Count == 0)
            return new RecentlyVisitedResponse([]);

        var hotelIds = recentVisits.Select(x => x.HotelId).ToList();

        var hotels = await context.Hotels
            .Include(h => h.City)
            .Where(h => hotelIds.Contains(h.Id))
            .Select(h => new
            {
                h.Id,
                h.Name,
                CityName = h.City.Name,
                h.StarRating,
                h.ThumbnailUrl,
                h.MinPricePerNight
            })
            .ToListAsync(ct);

        var hotelMap = hotels.ToDictionary(h => h.Id, h => h);

        var items = recentVisits
            .Where(v => hotelMap.ContainsKey(v.HotelId))
            .Select(v =>
            {
                var h = hotelMap[v.HotelId];
                return new RecentlyVisitedDto(
                    h.Id,
                    h.Name,
                    h.CityName,
                    h.StarRating,
                    h.ThumbnailUrl,
                    h.MinPricePerNight,
                    v.LatestVisit);
            })
            .ToList();

        return new RecentlyVisitedResponse(items);
    }
}