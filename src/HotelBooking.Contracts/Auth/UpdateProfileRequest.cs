namespace HotelBooking.Contracts.Auth;

public sealed record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber);
