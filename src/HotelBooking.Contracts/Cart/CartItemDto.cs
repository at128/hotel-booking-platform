namespace HotelBooking.Contracts.Cart;

public sealed record CartItemDto(
    Guid Id,
    Guid HotelId,
    string HotelName,
    Guid HotelRoomTypeId,
    string RoomTypeName,
    int MaxOccupancy,
    decimal PricePerNight,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,
    int Quantity,
    decimal Subtotal);