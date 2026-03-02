namespace HotelBooking.Infrastructure.Settings;

public sealed class CookieSettings
{
    public string RefreshTokenCookieName { get; set; } = "hb_rt";
    public int RefreshTokenExpiryDays { get; set; } = 7;
    public bool SecureOnly { get; set; } = true;      
    public string SameSite { get; set; } = "Strict";  
    public string Path { get; set; } = "/api";
}