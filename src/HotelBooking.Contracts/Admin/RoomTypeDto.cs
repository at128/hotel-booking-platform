namespace HotelBooking.Contracts.Admin;

public sealed record RoomTypeDto(
    Guid Id,
    string Name,
    string? Description,
    int HotelAssignmentCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModifiedAtUtc);