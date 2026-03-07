namespace HotelBooking.Contracts.Admin.HotelRoomTypes;

public sealed record HotelRoomTypeAdminDto(
    Guid Id,
    Guid HotelId,
    string HotelName,
    Guid RoomTypeId,
    string RoomTypeName,
    decimal PricePerNight,
    short AdultCapacity,
    short ChildCapacity,
    short MaxOccupancy,
    string? Description);