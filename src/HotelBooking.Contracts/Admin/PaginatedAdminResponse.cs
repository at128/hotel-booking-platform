namespace HotelBooking.Contracts.Admin;

public sealed record PaginatedAdminResponse<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore);