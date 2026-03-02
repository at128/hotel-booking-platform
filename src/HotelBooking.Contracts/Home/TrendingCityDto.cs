namespace HotelBooking.Contracts.Home;

public sealed record TrendingCityDto(
    Guid CityId,
    string Name,
    string Country,
    int HotelCount,
    int VisitCount);

public sealed record TrendingCitiesResponse(List<TrendingCityDto> Cities);