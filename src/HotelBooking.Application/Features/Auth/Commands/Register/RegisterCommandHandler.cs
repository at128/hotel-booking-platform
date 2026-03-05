using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Settings;
using HotelBooking.Contracts.Auth;
using HotelBooking.Domain.Common.Results;
using MediatR;
using Microsoft.Extensions.Options;

namespace HotelBooking.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler(
    IIdentityService identityService,
    ITokenProvider tokenProvider,
    IRefreshTokenRepository refreshTokenRepository,
    ICookieService cookieService,
    IOptions<RefreshTokenSettings> rtOptions) : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly RefreshTokenSettings _rtSettings = rtOptions.Value;
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

        var tokenResult = tokenProvider.GenerateJwtToken(appUser);
        if (tokenResult.IsError)
            return tokenResult.TopError;

        var rawRT = tokenProvider.GenerateRefreshToken();
        var rtHash = tokenProvider.HashToken(rawRT);
        var family = Guid.NewGuid().ToString("N");

        var rtData = new RefreshTokenData(
            Id: Guid.CreateVersion7(),
            UserId: user.Id,
            TokenHash: rtHash,
            Family: family,
            IsActive: true,
            IsUsed: false,
            IsRevoked: false,
            ExpiresAt: DateTimeOffset.UtcNow.AddDays(_rtSettings.ExpiryDays));

        await refreshTokenRepository.AddAsync(rtData, ct);

        cookieService.SetRefreshTokenCookie(rawRT);


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