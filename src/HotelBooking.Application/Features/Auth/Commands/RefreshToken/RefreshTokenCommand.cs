using HotelBooking.Contracts.Auth;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand : IRequest<Result<TokenResponse>>;