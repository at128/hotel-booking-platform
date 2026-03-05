using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Features.Checkout.Commands.CancelBooking;
using HotelBooking.Application.Features.Checkout.Queries.GetBooking;
using HotelBooking.Application.Features.Checkout.Queries.GetUserBookings;
using HotelBooking.Contracts.Admin;
using HotelBooking.Contracts.Checkout;
using HotelBooking.Contracts.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Controllers;

[Authorize]
public sealed class BookingsController(ISender sender, IUser currentUser) : ApiController
{
    /// <summary>
    /// Retrieves booking details by ID.
    /// The authenticated user must be the booking owner, or an Admin.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBooking(Guid id, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
            return Unauthorized();

        var isAdmin = User.IsInRole("Admin");

        var result = await sender.Send(new GetBookingQuery(id, userId, isAdmin), ct);
        return result.Match(Ok, Problem);
    }

    /// <summary>Get all bookings for the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<BookingListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyBookings(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
            return Unauthorized();

        var result = await sender.Send(
            new GetUserBookingsQuery(userId, status, page, pageSize),
            ct);

        return result.Match(Ok, Problem);
    }

    /// <summary>Cancel a confirmed booking. Free within certain hours given in settings; fee applies after free window.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(CancellationDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelBooking(
        Guid id,
        [FromBody] CancelBookingRequest request,
        CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
            return Unauthorized();

        var result = await sender.Send(
            new CancelBookingCommand(
                BookingId: id,
                RequestingUserId: userId,
                IsAdmin: User.IsInRole("Admin"),
                Reason: request.Reason),
            ct);

        return result.Match(Ok, Problem);
    }

}