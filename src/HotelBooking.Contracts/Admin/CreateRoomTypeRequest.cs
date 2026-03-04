namespace HotelBooking.Contracts.Admin;

public sealed record CreateRoomTypeRequest(
    string Name,
    string? Description);