namespace HotelBooking.Application.Settings;

public sealed class RefreshTokenSettings
{
    public int ExpiryDays { get; init; } = 7;
    public int TokenBytes { get; init; } = 64;
}