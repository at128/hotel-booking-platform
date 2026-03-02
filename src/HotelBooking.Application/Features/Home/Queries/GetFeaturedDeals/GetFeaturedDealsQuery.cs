using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Home;
using HotelBooking.Domain.Common.Results;

namespace HotelBooking.Application.Features.Home.Queries.GetFeaturedDeals;

public sealed record GetFeaturedDealsQuery() : ICachedQuery<Result<FeaturedDealsResponse>>
{
    public string CacheKey => "home:featured-deals";
    public string[] Tags => ["home", "featured-deals"];
    public TimeSpan Expiration => TimeSpan.FromMinutes(5);
}