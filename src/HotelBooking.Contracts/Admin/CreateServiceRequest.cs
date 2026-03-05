namespace HotelBooking.Contracts.Admin;

public sealed record CreateServiceRequest(
    string Name,
    string? Description);