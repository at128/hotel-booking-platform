using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IIdentityService identityService,
    IRefreshTokenRepository? refreshTokenRepository = null,
    ICookieService? cookieService = null)
    : IRequestHandler<ChangePasswordCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(
        ChangePasswordCommand cmd,
        CancellationToken ct)
    {
        var result = await identityService.ChangePasswordAsync(
            cmd.UserId,
            cmd.CurrentPassword,
            cmd.NewPassword,
            ct);

        if (result.IsError)
            return result;

        if (refreshTokenRepository is not null &&
            Guid.TryParse(cmd.UserId, out var userId))
        {
            await refreshTokenRepository.RevokeAllForUserAsync(userId, ct);
        }

        cookieService?.RemoveRefreshTokenCookie();

        return Result.Success;
    }
}
