// src/HotelBooking.Application/Features/Auth/Commands/LogoutAllSessions/LogoutAllSessionsCommandHandler.cs
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Auth.Commands.LogoutAllSessions;

public sealed class LogoutAllSessionsCommandHandler(
    IRefreshTokenRepository refreshTokenRepo,
    ICookieService cookieService)           
    : IRequestHandler<LogoutAllSessionsCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(
        LogoutAllSessionsCommand cmd, CancellationToken ct)
    {
        await refreshTokenRepo.RevokeAllForUserAsync(cmd.UserId, ct);
        cookieService.RemoveRefreshTokenCookie();

        return Result.Success;
    }
}