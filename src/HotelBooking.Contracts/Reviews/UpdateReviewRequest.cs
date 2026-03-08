namespace HotelBooking.Contracts.Reviews;

public sealed record UpdateReviewRequest(
    short Rating,
    string? Title,
    string? Comment);