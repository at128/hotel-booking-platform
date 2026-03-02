namespace HotelBooking.Contracts.Auth;

public record TokenResponse(
    string AccessToken,
    DateTime ExpiresOnUtc);