namespace HotelBooking.Contracts.Admin;

public sealed record PaymentListItemDto(
    Guid Id,
    Guid BookingId,
    string BookingNumber,
    decimal Amount,
    string Method,
    string Status,
    string? TransactionRef,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PaidAtUtc);