using System.Text.Json.Serialization;

namespace HotelBooking.Contracts.Search;

public sealed record SearchHotelsRequest(
    string? Query,
    string? City,
    DateOnly? CheckIn,
    DateOnly? CheckOut,
    int? Adults,
    int? Children,
    int? NumberOfRooms,
    decimal? MinPrice,
    decimal? MaxPrice,
    short? MinStarRating,
    IReadOnlyCollection<string>? Amenities,
    string? SortBy,
    string? Cursor,
    int Limit = 20);