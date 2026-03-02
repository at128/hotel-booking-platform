using HotelBooking.Domain.Common;
using HotelBooking.Domain.Rooms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Domain.Hotels
{
    public class HotelRoomType : AuditableEntity, ISoftDeletable
    {
        private HotelRoomType() { }

        public HotelRoomType(
            Guid id,
            Guid hotelId,
            Guid roomTypeId,
            decimal pricePerNight,
            short adultCapacity = 2,
            short childCapacity = 0,
            string? description = null)
            : base(id)
        {
            HotelId = hotelId;
            RoomTypeId = roomTypeId;
            PricePerNight = pricePerNight;
            AdultCapacity = adultCapacity;
            ChildCapacity = childCapacity;
            Description = description;
        }

        public Guid HotelId { get; private set; }
        public Guid RoomTypeId { get; private set; }
        public decimal PricePerNight { get; private set; }
        public short AdultCapacity { get; private set; }
        public short ChildCapacity { get; private set; }
        public short MaxOccupancy => (short)(AdultCapacity + ChildCapacity);
        public string? Description { get; private set; }
        public DateTimeOffset? DeletedAtUtc { get; set; }

        // Navigation
        public Hotel Hotel { get; private set; } = null!;
        public RoomType RoomType { get; private set; } = null!;
        public ICollection<Room> Rooms { get; private set; } = [];
        public ICollection<HotelRoomTypeService> HotelRoomTypeServices { get; private set; } = [];

        public void Update(
            decimal pricePerNight,
            short adultCapacity,
            short childCapacity,
            string? description)
        {
            PricePerNight = pricePerNight;
            AdultCapacity = adultCapacity;
            ChildCapacity = childCapacity;
            Description = description;
        }
    }

}
