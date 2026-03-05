namespace HotelBooking.Contracts.Common;

public sealed record PaginatedResponse<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore);