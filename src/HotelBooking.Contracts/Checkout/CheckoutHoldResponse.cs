namespace HotelBooking.Contracts.Checkout;

public sealed record CheckoutHoldResponse(
    List<Guid> HoldIds,
    Guid HotelId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,
    decimal Subtotal,
    decimal Tax,
    decimal Total,
    DateTimeOffset ExpiresAtUtc,
    string? Notes);