using HotelBooking.Domain.Rooms;

namespace HotelBooking.Contracts.Admin;

public sealed record RoomDto(
    Guid Id,
    Guid HotelId,
    string HotelName,
    Guid HotelRoomTypeId,
    string RoomTypeName,
    string RoomNumber,
    short? Floor,
    RoomStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastModifiedUtc);