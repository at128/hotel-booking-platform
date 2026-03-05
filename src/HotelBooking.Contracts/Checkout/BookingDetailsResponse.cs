namespace HotelBooking.Contracts.Checkout;

public sealed record BookingDetailsResponse(
    Guid BookingId,
    string BookingNumber,
    string HotelName,
    string HotelAddress,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,
    decimal TotalAmount,
    string Status,               
    string? Notes,
    List<BookingRoomDto> Rooms,
    BookingPaymentDto? Payment);

public sealed record BookingRoomDto(
    string RoomTypeName,
    string RoomNumber,
    decimal PricePerNight);

public sealed record BookingPaymentDto(
    string Status,
    string? TransactionRef,
    DateTimeOffset? PaidAtUtc);