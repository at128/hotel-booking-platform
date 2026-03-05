namespace HotelBooking.Contracts.Reviews;

public sealed record CreateHotelReviewRequest(
    short Rating,
    string? Title,
    string? Comment,
    Guid BookingId);