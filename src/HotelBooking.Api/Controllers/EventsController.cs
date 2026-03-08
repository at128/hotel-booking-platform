using System.Security.Claims;
using HotelBooking.Application.Features.Events.Commands.TrackHotelView;
using HotelBooking.Application.Features.Events.Queries.GetRecentlyVisited;
using HotelBooking.Contracts.Events;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HotelBooking.Api.Controllers;

[Authorize]
public sealed class EventsController(ISender sender) : ApiController
{
    [EnableRateLimiting("events")]
    [HttpPost("hotel-viewed")]
    [HttpPost("hotel-view")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> TrackHotelView(
        [FromBody] TrackHotelViewRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await sender.Send(new TrackHotelViewCommand(userId, request.HotelId), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [EnableRateLimiting("user-read")]
    [HttpGet("recently-visited")]
    public async Task<IActionResult> GetRecentlyVisited(CancellationToken ct)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await sender.Send(new GetRecentlyVisitedQuery(userId), ct);
        return result.Match(Ok, Problem);
    }
}
