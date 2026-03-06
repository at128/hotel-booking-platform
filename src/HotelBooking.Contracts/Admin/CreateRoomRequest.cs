using HotelBooking.Domain.Rooms;

namespace HotelBooking.Contracts.Admin;

public sealed record CreateRoomRequest(
    Guid HotelRoomTypeId,
    string RoomNumber,
    short? Floor,
    RoomStatus Status);