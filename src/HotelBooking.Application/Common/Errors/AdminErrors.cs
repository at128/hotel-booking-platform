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
    public static class Rooms
    {
        public static Error NotFound(Guid id) =>
            Error.NotFound("Admin.Rooms.NotFound", $"Room type assignment {id} was not found.");

        public static Error ReferencedRoomTypeNotFound(Guid id) =>
            Error.NotFound("Admin.Rooms.RoomTypeNotFound", $"Room type {id} was not found.");

        public static readonly Error AlreadyExists =
            Error.Conflict("Admin.Rooms.AlreadyExists",
                "This room type is already assigned to the selected hotel.");

        public static readonly Error HasActiveBookings =
            Error.Conflict("Admin.Rooms.HasActiveBookings",
                "Cannot delete a room type assignment that has confirmed bookings.");
    }
    public static class RoomTypes
    {
        public static Error NotFound(Guid id) =>
            Error.NotFound("Admin.RoomTypes.NotFound", $"Room type {id} was not found.");

        public static readonly Error AlreadyExists =
            Error.Conflict("Admin.RoomTypes.AlreadyExists",
                "A room type with the same name already exists.");

        public static readonly Error HasRelatedHotelAssignments =
            Error.Conflict("Admin.RoomTypes.HasRelatedHotelAssignments",
                "Cannot delete a room type that is assigned to one or more hotels.");
    }
    public static class Services
    {
        public static Error NotFound(Guid id) =>
            Error.NotFound("Admin.Services.NotFound", $"Service {id} was not found.");

        public static readonly Error AlreadyExists =
            Error.Conflict("Admin.Services.AlreadyExists",
                "A service with the same name already exists.");

        public static readonly Error HasRelatedHotelAssignments =
            Error.Conflict("Admin.Services.HasRelatedHotelAssignments",
                "Cannot delete a service that is assigned to one or more hotel room types.");
    }
}