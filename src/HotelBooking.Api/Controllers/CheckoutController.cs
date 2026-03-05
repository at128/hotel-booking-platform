using HotelBooking.Application.Features.Checkout.Commands.CreateBooking;
using HotelBooking.Application.Features.Checkout.Commands.CreateCheckoutHold;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Checkout;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBooking.Api.Controllers;

[Authorize]
public sealed class CheckoutController(ISender sender, IUser currentUser) : ApiController
{
    /// <summary>
    /// Step 1: Create a checkout hold from the user's cart.
    /// Returns pricing summary and hold expiry. Client should proceed to CreateBooking before expiry.
    /// </summary>
    [HttpPost("hold")]
    [ProducesResponseType(typeof(CheckoutHoldResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateHold(
        [FromBody] CreateHoldRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
            return Unauthorized();

        var result = await sender.Send(
            new CreateCheckoutHoldCommand(userId, request.Notes), ct);

        return result.Match(Ok, Problem);
    }

    /// <summary>
    /// Step 2: Confirm the booking and obtain a Stripe payment URL.
    /// Requires valid, non-expired checkout holds from Step 1.
    /// </summary>
    [HttpPost("booking")]
    [ProducesResponseType(typeof(CreateBookingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateBooking(
        [FromBody] CreateBookingRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
            return Unauthorized();

        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        var result = await sender.Send(
            new CreateBookingCommand(
                UserId: userId,
                UserEmail: email,
                HoldIds: request.HoldIds,
                Notes: request.Notes),
            ct);

        return result.Match(
            response => CreatedAtAction(nameof(CreateBooking), new { id = response.BookingId }, response),
            Problem);
    }
}

