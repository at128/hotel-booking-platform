using HotelBooking.Application.Features.Reviews.Commands.CreateHotelReview;
using HotelBooking.Contracts.Reviews;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/v{version:apiVersion}/hotels")]
public sealed class HotelsController(ISender sender) : ControllerBase
{
    [Authorize]
    [HttpPost("{hotelId:guid}/reviews")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateReview(
        Guid hotelId,
        [FromBody] CreateHotelReviewRequest request,
        CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await sender.Send(new CreateHotelReviewCommand(
            HotelId: hotelId,
            BookingId: request.BookingId,
            UserId: userId,
            Rating: request.Rating,
            Title: request.Title,
            Comment: request.Comment), ct);

        if (result.IsError)
            return Problem();

        return CreatedAtAction(
            actionName: nameof(CreateReview), 
            routeValues: new { hotelId, version = "1" },
            value: result.Value);
    }
}