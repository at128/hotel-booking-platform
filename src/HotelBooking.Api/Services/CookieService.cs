using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace HotelBooking.Api.Services;

public sealed class CookieService(
    IHttpContextAccessor httpContextAccessor,
    IOptions<CookieSettings> cookieOptions) : ICookieService
{
    private readonly CookieSettings _settings = cookieOptions.Value;
    private HttpContext HttpContext => httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("No active HttpContext.");

    public void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,                        
            Secure = _settings.SecureOnly,         
            SameSite = ParseSameSite(_settings.SameSite),
            Expires = DateTimeOffset.UtcNow.AddDays(_settings.RefreshTokenExpiryDays),
            Path = _settings.Path,                  
            IsEssential = true                      
        };

        HttpContext.Response.Cookies.Append(
            _settings.RefreshTokenCookieName,
            refreshToken,
            cookieOptions);
    }

    public string? GetRefreshTokenFromCookie()
    {
        HttpContext.Request.Cookies.TryGetValue(
            _settings.RefreshTokenCookieName,
            out var token);
        return token;
    }

    public void RemoveRefreshTokenCookie()
    {
        HttpContext.Response.Cookies.Append(
            _settings.RefreshTokenCookieName,
            string.Empty,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = _settings.SecureOnly,
                SameSite = ParseSameSite(_settings.SameSite),
                Expires = DateTimeOffset.UtcNow.AddDays(-1), 
                Path = _settings.Path
            });
    }

    private static SameSiteMode ParseSameSite(string value) => value.ToLower() switch
    {
        "strict" => SameSiteMode.Strict,
        "lax" => SameSiteMode.Lax,
        "none" => SameSiteMode.None,
        _ => SameSiteMode.Strict
    };
}