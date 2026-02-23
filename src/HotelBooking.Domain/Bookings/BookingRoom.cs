using HotelBooking.Domain.Common;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HotelBooking.Domain.Common.Constants.HotelBookingConstants;

namespace HotelBooking.Domain.Bookings
{
    public class BookingRoom : Entity
    {
        private BookingRoom() { }

        public BookingRoom(
            Guid id,
            Guid bookingId,
            Guid hotelId,
            Guid roomId,
            Guid hotelRoomTypeId,
            string roomTypeName,
            string roomNumber,
            decimal pricePerNight)
            : base(id)
        {
            BookingId = bookingId;
            HotelId = hotelId;
            RoomId = roomId;
            HotelRoomTypeId = hotelRoomTypeId;
            RoomTypeName = roomTypeName;
            RoomNumber = roomNumber;
            PricePerNight = pricePerNight;
        }

        public Guid BookingId { get; private set; }
        public Guid HotelId { get; private set; }
        public Guid RoomId { get; private set; }
        public Guid HotelRoomTypeId { get; private set; }

        public string RoomTypeName { get; private set; } = null!;
        public string RoomNumber { get; private set; } = null!;
        public decimal PricePerNight { get; private set; }

        public Booking Booking { get; private set; } = null!;
        public Room Room { get; private set; } = null!;
        public HotelRoomType HotelRoomType { get; private set; } = null!;
    }

}
