namespace HotelBooking.Contracts.Search;

public sealed record SearchHotelsResponse(
    List<SearchHotelDto> Items,
    string? NextCursor,
    bool HasMore,
    int Limit
);