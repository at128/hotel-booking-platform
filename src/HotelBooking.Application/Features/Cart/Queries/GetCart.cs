using HotelBooking.Contracts.Cart;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Cart.Queries.GetCart;

public sealed record GetCartQuery(Guid UserId) : IRequest<Result<CartResponse>>;