using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Cart.Commands.ClearCart;

public sealed record ClearCartCommand(Guid UserId) : IRequest<Result<Success>>;