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
            Error.NotFound("Admin.Rooms.NotFound", $"Room {id} was not found.");

        public static Error ReferencedHotelRoomTypeNotFound(Guid id) =>
            Error.NotFound("Admin.Rooms.HotelRoomTypeNotFound", $"Hotel room type {id} was not found.");

        public static readonly Error DuplicateRoomNumber =
            Error.Conflict("Admin.Rooms.DuplicateRoomNumber",
                "A room with the same number already exists in this hotel.");

        public static readonly Error InvalidStatus =
            Error.Validation("Admin.Rooms.InvalidStatus",
                "Invalid room status.");

        public static readonly Error HasActiveBookings =
            Error.Conflict("Admin.Rooms.HasActiveBookings",
                "Cannot delete a room that has confirmed bookings.");
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

    public static class HotelRoomTypes
    {
        public static readonly Error AlreadyExists =
            Error.Conflict("Admin.HotelRoomTypes.AlreadyExists", "Hotel room type already exists for this hotel and room type.");

        public static Error NotFound(Guid id) =>
            Error.NotFound("Admin.HotelRoomTypes.NotFound", $"Hotel room type '{id}' was not found.");
        
        public static readonly Error HasPendingBookings =
        Error.Conflict(
            "Admin.HotelRoomTypes.HasPendingBookings",
            "Hotel room type cannot be deleted while it has pending bookings.");

        public static readonly Error HasActiveHolds =
            Error.Conflict(
                "Admin.HotelRoomTypes.HasActiveHolds",
                "Hotel room type cannot be deleted while it has active checkout holds.");

        public static readonly Error HasAssignedRooms =
            Error.Conflict(
                "Admin.HotelRoomTypes.HasAssignedRooms",
                 "Hotel room type cannot be deleted while it still has assigned rooms.");
    }

}