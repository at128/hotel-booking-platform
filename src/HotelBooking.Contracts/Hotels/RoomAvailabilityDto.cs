namespace HotelBooking.Contracts.Hotels;

public sealed record RoomAvailabilityDto(
    Guid HotelRoomTypeId,
    string RoomTypeName,
    decimal PricePerNight,
    short AdultCapacity,
    short ChildCapacity,
    int TotalRooms,
    int BookedRooms,
    int HeldRooms,
    int AvailableRooms);

public sealed record RoomAvailabilityResponse(
    Guid HotelId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    List<RoomAvailabilityDto> RoomTypes);