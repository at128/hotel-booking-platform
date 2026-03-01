using HotelBooking.Application.Common.Models;
using HotelBooking.Contracts.Auth;
using HotelBooking.Domain.Common.Results;
using System.Security.Claims;

namespace HotelBooking.Application.Common.Interfaces;

public interface ITokenProvider
{
    Task<Result<TokenResponse>> GenerateJwtTokenAsync(AppUserDto user, CancellationToken ct = default);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}