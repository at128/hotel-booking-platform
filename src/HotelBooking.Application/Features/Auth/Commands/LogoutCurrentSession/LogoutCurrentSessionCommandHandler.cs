// src/HotelBooking.Application/Features/Auth/Commands/LogoutCurrentSession/LogoutCurrentSessionCommandHandler.cs
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Auth.Commands.LogoutCurrentSession;

public sealed class LogoutCurrentSessionCommandHandler(
    ITokenProvider tokenProvider,
    IRefreshTokenRepository refreshTokenRepo,
    ICookieService cookieService)          
    : IRequestHandler<LogoutCurrentSessionCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(
        LogoutCurrentSessionCommand cmd, CancellationToken ct)
    {
        var rawToken = cookieService.GetRefreshTokenFromCookie();

        if (!string.IsNullOrWhiteSpace(rawToken))
        {
            var tokenHash = tokenProvider.HashToken(rawToken);
            var stored = await refreshTokenRepo.GetByHashAsync(tokenHash, ct);

            if (stored is not null && stored.UserId == cmd.UserId)
                await refreshTokenRepo.RevokeAllFamilyAsync(stored.Family, ct);
        }

        cookieService.RemoveRefreshTokenCookie();

        return Result.Success;
    }
}