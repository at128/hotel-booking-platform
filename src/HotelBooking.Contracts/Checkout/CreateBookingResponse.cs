namespace HotelBooking.Contracts.Checkout;

public sealed record CreateBookingResponse(
    Guid BookingId,
    string BookingNumber,
    decimal TotalAmount,
    string PaymentUrl,
    DateTimeOffset ExpiresAtUtc);