using HotelBooking.Domain.Rooms;

namespace HotelBooking.Contracts.Admin;

public sealed record UpdateRoomRequest(
    string RoomNumber,
    short? Floor,
    RoomStatus Status);