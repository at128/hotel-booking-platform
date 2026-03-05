namespace HotelBooking.Application.Common.Models.Payment;

public sealed record RefundResponse(
    bool IsSuccess,
    string? RefundId,
    string? ErrorMessage);