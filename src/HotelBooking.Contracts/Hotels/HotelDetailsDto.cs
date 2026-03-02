namespace HotelBooking.Contracts.Hotels;

public sealed record HotelDetailsDto(
    Guid Id,
    string Name,
    string? Description,
    short StarRating,
    string Owner,
    string Address,
    decimal? Latitude,
    decimal? Longitude,
    string CheckInTime,
    string CheckOutTime,
    string CityName,
    string Country,
    double AverageRating,
    int ReviewCount,
    string? ThumbnailUrl,
    List<string> Amenities,
    List<HotelRoomTypeDto> RoomTypes);

public sealed record HotelRoomTypeDto(
    Guid HotelRoomTypeId,
    string RoomTypeName,
    string? Description,
    decimal PricePerNight,
    short AdultCapacity,
    short ChildCapacity);