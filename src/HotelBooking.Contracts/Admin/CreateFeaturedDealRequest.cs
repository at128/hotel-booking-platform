public sealed record CreateFeaturedDealRequest(
    Guid HotelId,
    Guid HotelRoomTypeId,
    decimal OriginalPrice,
    decimal DiscountedPrice,
    int DisplayOrder = 0,
    DateTimeOffset? StartsAtUtc = null,
    DateTimeOffset? EndsAtUtc = null);