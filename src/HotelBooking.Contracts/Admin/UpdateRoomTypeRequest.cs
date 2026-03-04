namespace HotelBooking.Contracts.Admin;

public sealed record UpdateRoomTypeRequest(
    string Name,
    string? Description);