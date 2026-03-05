namespace HotelBooking.Contracts.Checkout;

public sealed record CancellationDetailsResponse(
    Guid BookingId,
    string BookingNumber,
    decimal RefundAmount,
    decimal RefundPercentage,
    string RefundStatus,
    DateTimeOffset CancelledAtUtc,
    string? Reason);