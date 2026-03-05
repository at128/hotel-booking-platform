using HotelBooking.Contracts.Cart;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Cart.Queries;

public sealed record GetCartQuery(Guid UserId) : IRequest<Result<CartResponse>>;