namespace HotelBooking.Contracts.Admin;

public sealed record ServiceDto(
    Guid Id,
    string Name,
    string? Description,
    int HotelAssignmentCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModifiedAtUtc);