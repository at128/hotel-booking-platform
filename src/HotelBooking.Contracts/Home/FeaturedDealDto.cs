namespace HotelBooking.Contracts.Home;

public sealed record FeaturedDealDto(
    Guid DealId,
    Guid HotelId,
    string HotelName,
    string CityName,
    short StarRating,
    string? ThumbnailUrl,
    decimal OriginalPrice,
    decimal DiscountedPrice,
    int DiscountPercentage);

public sealed record FeaturedDealsResponse(List<FeaturedDealDto> Deals);