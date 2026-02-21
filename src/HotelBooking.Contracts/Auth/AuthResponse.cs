namespace HotelBooking.Contracts.Auth;

public sealed record AuthResponse(Guid Id,
                                  string Email,
                                  string FirstName,
                                  string LastName,
                                  string Role,
                                  DateTimeOffset CreatedAt,
                                  TokenResponse Token);
