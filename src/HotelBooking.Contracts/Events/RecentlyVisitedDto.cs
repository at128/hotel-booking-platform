namespace HotelBooking.Contracts.Events;

public sealed record RecentlyVisitedDto(
    Guid HotelId,
    string HotelName,
    string CityName,
    short StarRating,
    string? ThumbnailUrl,
    decimal? MinPricePerNight,
    DateTimeOffset VisitedAt);

public sealed record RecentlyVisitedResponse(List<RecentlyVisitedDto> Items);