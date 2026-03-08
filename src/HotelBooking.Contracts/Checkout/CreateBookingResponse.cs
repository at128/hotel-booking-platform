namespace HotelBooking.Contracts.Checkout;

public sealed record CreateBookingResponse(
    Guid BookingId,
    string BookingNumber,
    string HotelName,
    string HotelAddress,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,
    List<BookingRoomSummary> Rooms,
    decimal TotalAmount,
    string PaymentUrl,
    DateTimeOffset ExpiresAtUtc);

public sealed record BookingRoomSummary(
    string RoomTypeName,
    string RoomNumber,
    decimal PricePerNight);