namespace HotelBooking.Contracts.Admin.HotelRoomTypes;

public sealed record CreateHotelRoomTypeRequest(
    Guid HotelId,
    Guid RoomTypeId,
    decimal PricePerNight,
    short AdultCapacity,
    short ChildCapacity,
    short? MaxOccupancy,
    string? Description);