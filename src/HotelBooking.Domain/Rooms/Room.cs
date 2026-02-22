using HotelBooking.Domain.Common;
using HotelBooking.Domain.Hotels;

namespace HotelBooking.Domain.Rooms
{
    public class Room : AuditableEntity, ISoftDeletable
    {
        private Room() { }

        public Room(
            Guid id,
            Guid hotelRoomTypeId,
            Guid hotelId,
            string roomNumber,
            short? floor = null)
            : base(id)
        {
            HotelRoomTypeId = hotelRoomTypeId;
            HotelId = hotelId;
            RoomNumber = roomNumber;
            Floor = floor;
        }

        public Guid HotelRoomTypeId { get; private set; }
        public Guid HotelId { get; private set; }
        public string RoomNumber { get; private set; } = null!;
        public short? Floor { get; private set; }
        public string Status { get; private set; } = "available";
        public DateTimeOffset? DeletedAtUtc { get; set; }

        public HotelRoomType HotelRoomType { get; private set; } = null!;
        public Hotel Hotel { get; private set; } = null!;

        public void UpdateStatus(string status)
        {
            Status = status;
        }

        public void Update(string roomNumber, short? floor)
        {
            RoomNumber = roomNumber;
            Floor = floor;
        }
    }

}
