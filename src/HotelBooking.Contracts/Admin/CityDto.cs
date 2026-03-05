namespace HotelBooking.Contracts.Admin;

public sealed record CityDto(
    Guid Id,
    string Name,
    string Country,
    string? PostOffice,
    int HotelCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModifiedAtUtc);