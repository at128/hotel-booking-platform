namespace HotelBooking.Application.Common.Models;

public sealed record AppUserDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IList<string> Roles);