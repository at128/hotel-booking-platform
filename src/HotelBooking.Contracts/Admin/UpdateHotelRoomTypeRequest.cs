namespace HotelBooking.Contracts.Admin.HotelRoomTypes;

public sealed record UpdateHotelRoomTypeRequest(
    decimal PricePerNight,
    short AdultCapacity,
    short ChildCapacity,
    short? MaxOccupancy,
    string? Description);