using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Auth;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Auth.Queries.GetProfile;

public sealed class GetProfileQueryHandler(IIdentityService identityService)
    : IRequestHandler<GetProfileQuery, Result<ProfileResponse>>
{
    public async Task<Result<ProfileResponse>> Handle(GetProfileQuery query, CancellationToken ct)
    {
        var result = await identityService.GetUserByIdAsync(query.UserId, ct);

        if (result.IsError)
            return result.TopError;

        var user = result.Value;

        return new ProfileResponse(
            user.Id, user.Email, user.FirstName, user.LastName,
            user.PhoneNumber, user.Role, user.CreatedAtUtc, user.UpdatedAtUtc);
    }
}
