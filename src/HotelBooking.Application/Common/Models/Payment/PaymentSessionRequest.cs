namespace HotelBooking.Application.Common.Models.Payment;

public sealed record PaymentSessionRequest(
    Guid BookingId,
    string BookingNumber,
    decimal AmountInUsd,
    string CustomerEmail,
    string HotelName,
    DateOnly CheckIn,
    DateOnly CheckOut,

    string SuccessUrl,
    string CancelUrl
);