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
    IRefreshTokenRepository refreshTokenRepo,
    IIdentityService identityService,
    IOptions<RefreshTokenSettings> refreshTokenOptions)
    : IRequestHandler<RefreshTokenCommand, Result<TokenResponse>>
{
    public async Task<Result<TokenResponse>> Handle(
        RefreshTokenCommand cmd,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.RefreshToken))
            return ApplicationErrors.Auth.InvalidRefreshToken;

        var incomingRaw = cmd.RefreshToken.Trim();
        var tokenHash = tokenProvider.HashToken(incomingRaw);

        var stored = await refreshTokenRepo.GetByHashAsync(tokenHash, ct);

        if (stored is null)
            return ApplicationErrors.Auth.InvalidRefreshToken;

        if (stored.IsUsed)
        {
            await refreshTokenRepo.RevokeAllFamilyAsync(stored.Family, ct);
            await refreshTokenRepo.SaveChangesAsync(ct);

            return ApplicationErrors.Auth.RefreshTokenReuse; 
        }


        if (!stored.IsActive)
            return ApplicationErrors.Auth.InvalidRefreshToken;


        var userResult = await identityService.GetUserByIdAsync(stored.UserId, ct);

        if (userResult.IsError)
            return userResult.TopError;

        var user = userResult.Value;

        var appUser = new AppUserDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            [user.Role]);

        var newRawRefresh = tokenProvider.GenerateRefreshToken();
        var newHash = tokenProvider.HashToken(newRawRefresh);

        var claimed = await refreshTokenRepo.TryMarkAsUsedAsync(
            stored.Id,
            replacedByTokenHash: newHash,
            nowUtc: DateTimeOffset.UtcNow,
            ct);

        if (!claimed)
        {
            await refreshTokenRepo.RevokeAllFamilyAsync(stored.Family, ct);
            await refreshTokenRepo.SaveChangesAsync(ct);

            return ApplicationErrors.Auth.RefreshTokenReuse;
        }

        var accessResult = await tokenProvider.GenerateJwtTokenAsync(appUser, ct);

        if (accessResult.IsError)
            return accessResult.TopError;

        var replacement = new RefreshTokenData(
            Id: Guid.CreateVersion7(),
            UserId: stored.UserId,
            TokenHash: newHash,
            Family: stored.Family,
            IsActive: true,
            IsUsed: false,
            IsRevoked: false,
            ExpiresAt: DateTimeOffset.UtcNow.AddDays(refreshTokenOptions.Value.ExpiryDays)); 

        await refreshTokenRepo.AddAsync(replacement, ct);
        await refreshTokenRepo.SaveChangesAsync(ct);

        var access = accessResult.Value;

        return new TokenResponse(
            AccessToken: access.AccessToken,
            ExpiresOnUtc: access.ExpiresOnUtc,
            RefreshToken: newRawRefresh);
    }
}