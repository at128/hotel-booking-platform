using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Common.Settings;
using HotelBooking.Contracts.Auth;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HotelBooking.Infrastructure.Identity;

public class TokenProvider(
    IOptions<JwtSettings> jwtSettings,
    IRefreshTokenRepository refreshTokenRepository,
    IOptions<RefreshTokenSettings> refreshTokenOptions) : ITokenProvider
{
    private readonly JwtSettings _jwt = jwtSettings.Value;
    private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
    private readonly RefreshTokenSettings _refreshTokenOptions = refreshTokenOptions.Value;

    public async Task<Result<TokenResponse>> GenerateJwtTokenAsync(
        AppUserDto user, CancellationToken ct = default)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(_jwt.ExpiryHours);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email,          user.Email),
            new(ClaimTypes.GivenName,      user.FirstName),
            new(ClaimTypes.Surname,        user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in user.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var response = new TokenResponse(
                AccessToken: new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresOnUtc: expiresAt.UtcDateTime);

        return response;
    }

    public async Task<Result<TokenResponse>> GenerateTokenPairAsync(AppUserDto user, string? existingFamily = null, string? deviceInfo = null, CancellationToken ct = default)
    {
        var accessResult = await GenerateJwtTokenAsync(user, ct);

        if (accessResult.IsError)
            return accessResult.TopError;

        var rawRefreshToken = GenerateRefreshToken();
        var tokenHash = HashToken(rawRefreshToken);
        var family = existingFamily ?? Guid.NewGuid().ToString("N");

        var refreshTokenData = new RefreshTokenData(
            Id: Guid.CreateVersion7(),
            UserId: user.UserId,
            TokenHash: tokenHash,
            Family: family,
            IsActive: true,
            IsUsed: false,
            IsRevoked: false,
            ExpiresAt: DateTimeOffset.UtcNow.AddDays(_refreshTokenOptions.ExpiryDays),
            DeviceInfo: deviceInfo);

        await refreshTokenRepository.AddAsync(refreshTokenData, ct);
        await refreshTokenRepository.SaveChangesAsync(ct);

        var access = accessResult.Value;

        return new TokenResponse(
            AccessToken: access.AccessToken,
            ExpiresOnUtc: access.ExpiresOnUtc,
            RefreshToken: rawRefreshToken);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,   
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwt.Issuer,
            ValidAudience = _jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(_jwt.Secret))
        };

        try
        {
            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(token, parameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }
    public string GenerateRefreshToken()
    {
        return RefreshTokenCrypto.GenerateRawToken(_refreshTokenOptions.TokenBytes);
    }

    public string HashToken(string token)
    {
        return RefreshTokenCrypto.HashToken(token);
    }


}