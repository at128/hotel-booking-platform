namespace HotelBooking.Application.Common.Models.Payment;

public sealed record PaymentSessionResponse(
    string SessionId,

    string PaymentUrl
);