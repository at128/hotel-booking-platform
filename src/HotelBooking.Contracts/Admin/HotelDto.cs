namespace HotelBooking.Contracts.Admin;

public sealed record HotelDto(
    Guid Id,
    Guid CityId,
    string CityName,
    string Name,
    string Owner,
    string Address,
    short StarRating,
    string? Description,
    decimal? Latitude,
    decimal? Longitude,
    decimal? MinPricePerNight,
    double AverageRating,
    int ReviewCount,
    int RoomTypeCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModifiedAtUtc);