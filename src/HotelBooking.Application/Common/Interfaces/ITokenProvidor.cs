using HotelBooking.Application.Common.Models;
using HotelBooking.Contracts.Auth;
using HotelBooking.Domain.Common.Results;
using System.Security.Claims;

namespace HotelBooking.Application.Common.Interfaces;

public interface ITokenProvider
{
    Task<Result<TokenResponse>> GenerateJwtTokenAsync(AppUserDto user, CancellationToken ct = default);
    Task<Result<TokenResponse>> GenerateTokenPairAsync(
        AppUserDto user,
        string? existingFamily = null,
        string? deviceInfo = null,
        CancellationToken ct = default);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}