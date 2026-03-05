namespace HotelBooking.Contracts.Admin;

public sealed record UpdateRoomRequest(
    decimal PricePerNight,
    short AdultCapacity,
    short ChildCapacity,
    string? Description);