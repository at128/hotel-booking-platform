namespace HotelBooking.Contracts.Reviews;

public sealed record ReviewDto(
    Guid Id,
    Guid HotelId,
    Guid BookingId,
    Guid UserId,
    string? ReviewerName,
    short Rating,
    string? Title,
    string? Comment,
    DateTimeOffset CreatedAtUtc);