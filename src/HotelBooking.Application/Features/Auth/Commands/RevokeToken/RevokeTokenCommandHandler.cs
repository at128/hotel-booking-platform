using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Auth.Commands.RevokeToken;

public sealed class RevokeTokenCommandHandler(
    ITokenProvider tokenProvider,
    IRefreshTokenRepository refreshTokenRepo)
    : IRequestHandler<RevokeTokenCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(RevokeTokenCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.RefreshToken))
            return Result.Success;

        var tokenHash = tokenProvider.HashToken(cmd.RefreshToken.Trim());
        var stored = await refreshTokenRepo.GetByHashAsync(tokenHash, ct);

        if (stored is null || stored.UserId != cmd.UserId)
            return Result.Success;

        await refreshTokenRepo.RevokeAllFamilyAsync(stored.Family, ct);
        await refreshTokenRepo.SaveChangesAsync(ct);

        return Result.Success;
    }
}