using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Home;
using HotelBooking.Domain.Common.Results;

namespace HotelBooking.Application.Features.Home.Queries.GetTrendingCities;

public sealed record GetTrendingCitiesQuery() : ICachedQuery<Result<TrendingCitiesResponse>>
{
    public string CacheKey => "home:trending-cities";
    public string[] Tags => ["home", "trending-cities"];
    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}