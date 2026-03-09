using HotelBooking.Api.Controllers;
using HotelBooking.Application.Features.Hotels.Queries.GetHotelDetails;
using HotelBooking.Application.Features.Hotels.Queries.GetHotelGallery;
using HotelBooking.Application.Features.Hotels.Queries.GetRoomAvailability;
using HotelBooking.Application.Features.Reviews.Commands.CreateHotelReview;
using HotelBooking.Application.Features.Reviews.Commands.DeleteReview;
using HotelBooking.Application.Features.Reviews.Commands.UpdateReview;
using HotelBooking.Application.Features.Reviews.Queries.GetHotelReviews;
using HotelBooking.Contracts.Hotels;
using HotelBooking.Contracts.Reviews;
using HotelBooking.Domain.Common.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/v{version:apiVersion}/hotels")]
[EnableRateLimiting("public-read")]
public sealed class HotelsController(ISender sender) : ApiController
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(HotelDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHotelDetails(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetHotelDetailsQuery(id), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{id:guid}/gallery")]
    [ProducesResponseType(typeof(HotelGalleryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHotelGallery(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetHotelGalleryQuery(id), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{id:guid}/room-availability")]
    [ProducesResponseType(typeof(RoomAvailabilityResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoomAvailability(
        Guid id,
        [FromQuery] DateOnly checkIn,
        [FromQuery] DateOnly checkOut,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetRoomAvailabilityQuery(id, checkIn, checkOut), ct);
        return result.Match(Ok, Problem);
    }

    [Authorize]
    [EnableRateLimiting("user-write")]
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
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new CreateHotelReviewCommand(
            HotelId: hotelId,
            BookingId: request.BookingId,
            UserId: userId,
            Rating: request.Rating,
            Title: request.Title,
            Comment: request.Comment), ct);

        if (result.IsError)
            return Problem(result.Errors);

        return CreatedAtAction(
            actionName: nameof(CreateReview),
            routeValues: new { hotelId, version = "1" },
            value: result.Value);
    }

    [HttpGet("{id:guid}/reviews")]
    [ProducesResponseType(typeof(HotelReviewsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHotelReviews(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetHotelReviewsQuery(id, page, pageSize), ct);
        return result.Match(Ok, Problem);
    }

    [Authorize]
    [EnableRateLimiting("user-write")]
    [HttpPut("{hotelId:guid}/reviews/{reviewId:guid}")]
    public async Task<IActionResult> UpdateReview(
        Guid hotelId,
        Guid reviewId,
        [FromBody] UpdateReviewRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new UpdateReviewCommand(
            hotelId, reviewId, userId, request.Rating, request.Title, request.Comment), ct);

        return result.Match(Ok, Problem);
    }

    [Authorize]
    [EnableRateLimiting("user-write")]
    [HttpDelete("{hotelId:guid}/reviews/{reviewId:guid}")]
    public async Task<IActionResult> DeleteReview(
        Guid hotelId, Guid reviewId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var isAdmin = User.IsInRole(HotelBookingConstants.Roles.Admin);
        var result = await sender.Send(
            new DeleteReviewCommand(hotelId, reviewId, userId, isAdmin), ct);

        return result.Match(_ => NoContent(), Problem);
    }
}
