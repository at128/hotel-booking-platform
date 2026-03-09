using HotelBooking.Application.Features.Cart.Commands.AddToCart;
using HotelBooking.Application.Features.Cart.Commands.ClearCart;
using HotelBooking.Application.Features.Cart.Commands.RemoveCartItem;
using HotelBooking.Application.Features.Cart.Commands.UpdateCartItem;
using HotelBooking.Application.Features.Cart.Queries;
using HotelBooking.Contracts.Cart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HotelBooking.Api.Controllers;

[Authorize]
[EnableRateLimiting("user-read")]
public sealed class CartController(ISender sender) : ApiController
{
    /// <summary>Get the current user's cart.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new GetCartQuery(userId), ct);
        return result.Match(Ok, Problem);
    }

    /// <summary>Add a room type to the cart.</summary>
    [EnableRateLimiting("user-write")]
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddToCart(
        [FromBody] AddToCartRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new AddToCartCommand(
            userId,
            request.HotelRoomTypeId,
            request.CheckIn,
            request.CheckOut,
            request.Quantity,
            request.Adults,
            request.Children), ct);

        return result.Match(
            item => CreatedAtAction(nameof(GetCart), item),
            Problem);
    }

    /// <summary>Update quantity for a specific cart item.</summary>
    [EnableRateLimiting("user-write")]
    [HttpPut("items/{itemId:guid}")]
    [ProducesResponseType(typeof(CartItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCartItem(
        Guid itemId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(
            new UpdateCartItemCommand(userId, itemId, request.Quantity), ct);

        return result.Match(Ok, Problem);
    }

    /// <summary>Remove a specific item from the cart.</summary>
    [EnableRateLimiting("user-write")]
    [HttpDelete("items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveCartItem(
        Guid itemId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(
            new RemoveCartItemCommand(userId, itemId), ct);

        return result.Match(_ => NoContent(), Problem);
    }

    /// <summary>Clear all items from the cart.</summary>
    [EnableRateLimiting("user-write")]
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCart(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new ClearCartCommand(userId), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
