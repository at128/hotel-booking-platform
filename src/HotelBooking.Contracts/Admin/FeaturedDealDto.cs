namespace HotelBooking.Contracts.Admin;

public sealed record FeaturedDealDto(
    Guid Id,
    Guid HotelId,
    string HotelName,
    Guid HotelRoomTypeId,
    decimal OriginalPrice,
    decimal DiscountedPrice,
    int DisplayOrder,
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    bool IsActive);