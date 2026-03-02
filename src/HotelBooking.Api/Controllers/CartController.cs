using HotelBooking.Application.Features.Cart.Commands.AddToCart;
using HotelBooking.Application.Features.Cart.Commands.ClearCart;
using HotelBooking.Application.Features.Cart.Commands.RemoveCartItem;
using HotelBooking.Application.Features.Cart.Commands.UpdateCartItem;
using HotelBooking.Application.Features.Cart.Queries.GetCart;
using HotelBooking.Contracts.Cart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace HotelBooking.Api.Controllers;

[Authorize]
public sealed class CartController(ISender sender) : ApiController
{

    /// <summary>Get the current user's cart.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await sender.Send(new GetCartQuery(userId.Value), ct);
        return result.Match(Ok, Problem);
    }

    /// <summary>Add a room type to the cart.</summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddToCart(
        [FromBody] AddToCartRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await sender.Send(new AddToCartCommand(
            userId.Value,
            request.HotelRoomTypeId,
            request.CheckIn,
            request.CheckOut,
            request.Quantity), ct);

        return result.Match(
            item => CreatedAtAction(nameof(GetCart), item),
            Problem);
    }

    /// <summary>Update quantity for a specific cart item.</summary>
    [HttpPut("items/{itemId:guid}")]
    [ProducesResponseType(typeof(CartItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCartItem(
        Guid itemId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await sender.Send(
            new UpdateCartItemCommand(userId.Value, itemId, request.Quantity), ct);

        return result.Match(Ok, Problem);
    }

    /// <summary>Remove a specific item from the cart.</summary>
    [HttpDelete("items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveCartItem(
        Guid itemId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await sender.Send(
            new RemoveCartItemCommand(userId.Value, itemId), ct);

        return result.Match(_ => NoContent(), Problem);
    }

    /// <summary>Clear all items from the cart.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCart(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await sender.Send(new ClearCartCommand(userId.Value), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    private Guid? GetUserId()
    {
        var str = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(str, out var id) ? id : null;
    }
}