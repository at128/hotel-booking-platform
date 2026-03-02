using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Common.Settings;
using HotelBooking.Contracts.Auth;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.Extensions.Options;

namespace HotelBooking.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    ITokenProvider tokenProvider,
    IRefreshTokenRepository refreshTokenRepository,
    IIdentityService identityService,
    ICookieService cookieService,
    IOptions<RefreshTokenSettings> rtOptions)
    : IRequestHandler<RefreshTokenCommand, Result<TokenResponse>>
{
    private readonly RefreshTokenSettings _rtSettings = rtOptions.Value;

    public async Task<Result<TokenResponse>> Handle(
        RefreshTokenCommand cmd, CancellationToken ct)
    {
        var rawToken = cookieService.GetRefreshTokenFromCookie();
       if (string.IsNullOrWhiteSpace(rawToken))
             return ApplicationErrors.Auth.InvalidRefreshToken;

        var tokenHash = tokenProvider.HashToken(rawToken);
        var stored = await refreshTokenRepository.GetByHashAsync(tokenHash, ct);

        if (stored is null)
            return ApplicationErrors.Auth.InvalidRefreshToken;

        if (stored.IsUsed)
        {
            await refreshTokenRepository.RevokeAllFamilyAsync(stored.Family, ct);
            cookieService.RemoveRefreshTokenCookie();
            return ApplicationErrors.Auth.RefreshTokenReuse;
        }

        if (!stored.IsActive)
        {
            cookieService.RemoveRefreshTokenCookie();
            return ApplicationErrors.Auth.InvalidRefreshToken;
        }

        var userResult = await identityService.GetUserByIdAsync(stored.UserId, ct);
        if (userResult.IsError) return userResult.TopError;

        var user = userResult.Value;
        var appUser = new AppUserDto(
            user.Id, user.Email,
            user.FirstName, user.LastName,
            [user.Role]);

        var newTokenResult = tokenProvider.GenerateJwtToken(appUser);
        if (newTokenResult.IsError) return newTokenResult.TopError;

        var newRawRT = tokenProvider.GenerateRefreshToken();
        var newRTHash = tokenProvider.HashToken(newRawRT);

        var newRTData = new RefreshTokenData(
            Id: Guid.CreateVersion7(),
            UserId: user.Id,
            TokenHash: newRTHash,
            Family: stored.Family, 
            IsActive: true,
            IsUsed: false,
            IsRevoked: false,
            ExpiresAt: DateTimeOffset.UtcNow.AddDays(_rtSettings.ExpiryDays));

        var rotated = await refreshTokenRepository.RotateAsync(
            oldTokenId: stored.Id,
            oldTokenFamily: stored.Family,
            newToken: newRTData,
            nowUtc: DateTimeOffset.UtcNow,
            ct: ct);

        if (!rotated)
        {
            // Race condition: someone else used this token between our check and rotation
            await refreshTokenRepository.RevokeAllFamilyAsync(stored.Family, ct);
            cookieService.RemoveRefreshTokenCookie();
            return ApplicationErrors.Auth.RefreshTokenReuse;
        }


        cookieService.SetRefreshTokenCookie(newRawRT);

        return newTokenResult.Value;
    }
}