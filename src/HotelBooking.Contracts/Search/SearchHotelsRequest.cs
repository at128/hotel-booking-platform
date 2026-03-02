namespace HotelBooking.Contracts.Search;

public sealed record SearchHotelsRequest(
    string? City,
    DateOnly? CheckIn,
    DateOnly? CheckOut,
    int? Adults,
    int? Children,
    int? NumberOfRooms,
    decimal? MinPrice,
    decimal? MaxPrice,
    int? MinStarRating,
    List<string>? Amenities,
    string? SortBy,
    string? Cursor,
    int? Limit
);