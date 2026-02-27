namespace HotelBooking.Application.Common.Interfaces;

public interface ICookieService
{
    void SetRefreshTokenCookie(string refreshToken);
    string? GetRefreshTokenFromCookie();
    void RemoveRefreshTokenCookie();
}