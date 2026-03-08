namespace HotelBooking.Contracts.Reviews;

public sealed record HotelReviewsResponse(
    IReadOnlyList<ReviewDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore);