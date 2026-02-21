namespace HotelBooking.Contracts.Auth;

public sealed record ProfileResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string Role,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
