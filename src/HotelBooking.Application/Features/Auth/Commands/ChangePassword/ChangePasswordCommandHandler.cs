using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(IIdentityService identityService)
    : IRequestHandler<ChangePasswordCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(
        ChangePasswordCommand cmd,
        CancellationToken ct)
    {
        return await identityService.ChangePasswordAsync(
            cmd.UserId,
            cmd.CurrentPassword,
            cmd.NewPassword,
            ct);
    }
}
