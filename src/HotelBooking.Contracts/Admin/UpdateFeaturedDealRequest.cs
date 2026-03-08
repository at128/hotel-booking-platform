public sealed record UpdateFeaturedDealRequest(
    decimal OriginalPrice,
    decimal DiscountedPrice,
    int DisplayOrder = 0,
    DateTimeOffset? StartsAtUtc = null,
    DateTimeOffset? EndsAtUtc = null);