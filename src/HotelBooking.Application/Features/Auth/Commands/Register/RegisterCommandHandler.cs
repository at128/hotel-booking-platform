using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Contracts.Auth;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler(
    IIdentityService identityService,
    ITokenProvider tokenProvider) : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(
        RegisterCommand request, CancellationToken ct)
    {

        var registerResult = await identityService.RegisterUserAsync(
            request.Email, request.Password,
            request.FirstName, request.LastName,
            request.PhoneNumber, ct);

        if (registerResult.IsError)
            return registerResult.TopError;

        var user = registerResult.Value;


        var appUser = new AppUserDto(
            user.Id, user.Email,
            user.FirstName, user.LastName,
            user.Roles);

        var tokenResult = await tokenProvider.GenerateJwtTokenAsync(appUser, ct);
        if (tokenResult.IsError)
            return tokenResult.TopError;


        return new AuthResponse(
            Id: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Role: user.Roles.FirstOrDefault() ?? "User",
            CreatedAt: user.CreatedAt,
            Token: tokenResult.Value);
    }
}