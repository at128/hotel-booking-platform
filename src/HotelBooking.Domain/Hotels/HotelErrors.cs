using HotelBooking.Domain.Common.Results;

namespace HotelBooking.Domain.Hotels;

public static class HotelErrors
{
    public static Error NotFound => Error.NotFound("Hotel.NotFound", "Hotel was not found.");
    public static Error HasActiveBookings => Error.Conflict("ADMIN_002", "Cannot delete hotel with active bookings.");
}