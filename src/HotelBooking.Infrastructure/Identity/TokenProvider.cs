// src/HotelBooking.Infrastructure/Identity/TokenProvider.cs
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Contracts.Auth;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HotelBooking.Infrastructure.Identity;

public sealed class TokenProvider(
    IOptions<JwtSettings> jwtSettings) : ITokenProvider
{
    private readonly JwtSettings _jwt = jwtSettings.Value;

    public Result<TokenResponse> GenerateJwtToken(AppUserDto user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwt.ExpiryMinutes);

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

        return new TokenResponse(
            AccessToken: new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresOnUtc: expiresAt.UtcDateTime);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
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
}