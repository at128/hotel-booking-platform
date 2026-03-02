using Asp.Versioning;
using HotelBooking.Application.Features.Home.Queries.GetFeaturedDeals;
using HotelBooking.Application.Features.Home.Queries.GetSearchConfig;
using HotelBooking.Application.Features.Home.Queries.GetTrendingCities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Controllers;

public sealed class HomeController(ISender sender) : ApiController
{
    /// <summary>
    /// GET /api/v1/home/featured-deals — Active deals (cached 5 min)
    /// </summary>
    [HttpGet("featured-deals")]
    public async Task<IActionResult> GetFeaturedDeals(CancellationToken ct)
    {
        var result = await sender.Send(new GetFeaturedDealsQuery(), ct);
        return result.Match(Ok, Problem);
    }

    /// <summary>
    /// GET /api/v1/home/trending-cities — Top 5 by visits (cached 10 min)
    /// </summary>
    [HttpGet("trending-cities")]
    public async Task<IActionResult> GetTrendingCities(CancellationToken ct)
    {
        var result = await sender.Send(new GetTrendingCitiesQuery(), ct);
        return result.Match(Ok, Problem);
    }

    /// <summary>
    /// GET /api/v1/home/config — Search defaults + amenities list
    /// </summary>
    [HttpGet("config")]
    public async Task<IActionResult> GetConfig(CancellationToken ct)
    {
        var result = await sender.Send(new GetSearchConfigQuery(), ct);
        return result.Match(Ok, Problem);
    }
}