using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Cart.Commands.RemoveCartItem;

public sealed record RemoveCartItemCommand(
    Guid UserId,
    Guid CartItemId) : IRequest<Result<Success>>;