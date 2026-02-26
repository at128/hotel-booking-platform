using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Auth.Commands.RevokeToken;

public sealed record RevokeTokenCommand(Guid UserId, string RefreshToken)
    : IRequest<Result<Success>>;