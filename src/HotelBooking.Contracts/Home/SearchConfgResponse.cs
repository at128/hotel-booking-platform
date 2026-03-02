namespace HotelBooking.Contracts.Home;

public sealed record SearchConfigResponse(
    int DefaultAdults,
    int DefaultChildren,
    int DefaultRooms,
    int MaxRooms,
    int MaxAdvanceBookingDays,
    decimal TaxRate,
    List<string> AvailableAmenities);