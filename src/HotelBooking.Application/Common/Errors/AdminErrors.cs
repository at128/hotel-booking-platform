using HotelBooking.Domain.Common.Results;

namespace HotelBooking.Application.Common.Errors;

public static class AdminErrors
{
    public static class Cities
    {
        public static readonly Error NotFound =
            Error.NotFound("Admin.Cities.NotFound", "City not found.");

        public static readonly Error AlreadyExists =
            Error.Conflict("Admin.Cities.AlreadyExists",
                "A city with the same name and country already exists.");

        public static readonly Error HasRelatedHotels =
            Error.Conflict("Admin.Cities.HasRelatedHotels",
                "Cannot delete city because it has related hotels.");
    }
    public static class Hotels
    {
        public static readonly Error NotFound =
            Error.NotFound("Admin.Hotels.NotFound", "Hotel not found.");

        public static readonly Error AlreadyExists =
            Error.Conflict("Admin.Hotels.AlreadyExists",
                "A hotel with the same name already exists in this city.");

        public static readonly Error HasRelatedRoomTypes =
            Error.Conflict("Admin.Hotels.HasRelatedRoomTypes",
                "Cannot delete hotel because it has related room types.");

        public static readonly Error HasActiveBookings =
            Error.Conflict("Admin.Hotels.HasActiveBookings",
                "Cannot delete hotel because it Active Bookings.");

        
    }

}