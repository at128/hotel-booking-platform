namespace HotelBooking.Contracts.Admin;

public sealed record CreateCityRequest(
    string Name,
    string Country,
    string? PostOffice);