namespace HotelBooking.Contracts.Home;

public sealed record TrendingCityDto(
    Guid Id,
    string Name,
    string Country,
    int HotelCount,
    int VisitCount,
    string? ThumbnailUrl);

public sealed record TrendingCitiesResponse(List<TrendingCityDto> Cities);