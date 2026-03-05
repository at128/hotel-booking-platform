namespace HotelBooking.Contracts.Admin;

public sealed record RoomDto(
    Guid Id,
    Guid HotelId,
    string HotelName,
    Guid RoomTypeId,
    string RoomTypeName,
    decimal PricePerNight,
    short AdultCapacity,
    short ChildCapacity,
    short MaxOccupancy,
    string? Description,
    int RoomCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModifiedAtUtc);