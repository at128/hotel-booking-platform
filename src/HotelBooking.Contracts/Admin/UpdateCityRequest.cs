namespace HotelBooking.Contracts.Admin;

public sealed record UpdateCityRequest(
    string Name,
    string Country,
    string? PostOffice);