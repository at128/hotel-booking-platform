namespace HotelBooking.Contracts.Cart;

public sealed record CartResponse(
    Guid? HotelId,
    string? HotelName,
    DateOnly? CheckIn,
    DateOnly? CheckOut,
    int TotalNights,
    List<CartItemDto> Items,
    decimal Total,
    int TotalRooms);