using HotelBooking.Contracts.Cart;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Cart.Commands.AddToCart;

public sealed record AddToCartCommand(
    Guid UserId,
    Guid HotelRoomTypeId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Quantity) : IRequest<Result<CartItemDto>>;