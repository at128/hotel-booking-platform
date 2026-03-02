namespace HotelBooking.Contracts.Search;

public sealed record SearchHotelDto(
    Guid HotelId,
    string Name,
    short StarRating,
    string? Description,
    string CityName,
    string Country,
    double AverageRating,
    int ReviewCount,
    string? ThumbnailUrl,
    decimal MinPricePerNight,
    List<string> Amenities
);