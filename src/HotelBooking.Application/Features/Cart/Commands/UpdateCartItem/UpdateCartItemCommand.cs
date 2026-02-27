using HotelBooking.Contracts.Cart;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Cart.Commands.UpdateCartItem;

public sealed record UpdateCartItemCommand(
    Guid UserId,
    Guid CartItemId,
    int Quantity) : IRequest<Result<CartItemDto>>;