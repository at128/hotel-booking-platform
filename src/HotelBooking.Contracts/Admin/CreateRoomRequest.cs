namespace HotelBooking.Contracts.Admin;

public sealed record CreateRoomRequest(
    Guid HotelId,
    Guid RoomTypeId,
    decimal PricePerNight,
    short AdultCapacity,
    short ChildCapacity,
    string? Description);