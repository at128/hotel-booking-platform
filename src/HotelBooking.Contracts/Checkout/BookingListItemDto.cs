namespace HotelBooking.Contracts.Checkout;

public sealed record BookingListItemDto(
    Guid BookingId,
    string BookingNumber,
    string HotelName,
    DateOnly CheckIn,
    DateOnly CheckOut,
    decimal TotalAmount,
    string Status,
    string? PaymentStatus);