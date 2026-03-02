using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Auth;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Auth.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler(IIdentityService identityService)
    : IRequestHandler<UpdateProfileCommand, Result<ProfileResponse>>
{
    public async Task<Result<ProfileResponse>> Handle(UpdateProfileCommand cmd, CancellationToken ct)
    {
        var result = await identityService.UpdateUserAsync(
            cmd.UserId, cmd.FirstName, cmd.LastName, cmd.PhoneNumber, ct);

        if (result.IsError)
            return result.TopError;

        var user = result.Value;

        return new ProfileResponse(
            user.Id, user.Email, user.FirstName, user.LastName,
            user.PhoneNumber, user.Role, user.CreatedAtUtc, user.UpdatedAtUtc);
    }
}
