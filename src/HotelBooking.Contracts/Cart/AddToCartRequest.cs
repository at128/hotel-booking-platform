namespace HotelBooking.Contracts.Cart;

public sealed record AddToCartRequest(
    Guid HotelRoomTypeId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Quantity = 1,
    int Adults = 2,
    int Children = 0);