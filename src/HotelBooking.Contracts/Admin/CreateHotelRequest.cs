namespace HotelBooking.Contracts.Admin;

public sealed record CreateHotelRequest(
    Guid CityId,
    string Name,
    string Owner,
    string Address,
    short StarRating,
    string? Description,
    decimal? Latitude,
    decimal? Longitude);