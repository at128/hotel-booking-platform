using HotelBooking.Application.Common.Models;
using HotelBooking.Contracts.Auth;
using HotelBooking.Domain.Common.Results;
using System.Security.Claims;

namespace HotelBooking.Application.Common.Interfaces;

public interface ITokenProvider
{
    Result<TokenResponse> GenerateJwtToken(AppUserDto user);

    string GenerateRefreshToken();
    string HashToken(string token);

    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}