namespace HotelBooking.Contracts.Checkout;

public sealed record CreateBookingRequest(
    List<Guid> HoldIds,
    string? Notes);