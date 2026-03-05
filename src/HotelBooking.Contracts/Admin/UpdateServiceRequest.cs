namespace HotelBooking.Contracts.Admin;

public sealed record UpdateServiceRequest(
    string Name,
    string? Description);