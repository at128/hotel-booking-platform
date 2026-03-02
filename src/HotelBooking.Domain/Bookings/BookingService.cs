using HotelBooking.Domain.Common;
using HotelBooking.Domain.Hotels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Domain.Bookings
{
    public class BookingService : Entity
    {
        private BookingService() { }

        public BookingService(
            Guid id,
            Guid bookingId,
            Guid hotelServiceId,
            Guid hotelId,
            string serviceName,
            decimal priceAtBooking,
            int quantity = 1)
            : base(id)
        {
            BookingId = bookingId;
            HotelServiceId = hotelServiceId;
            HotelId = hotelId;
            ServiceName = serviceName;
            PriceAtBooking = priceAtBooking;
            Quantity = quantity;
        }

        public Guid BookingId { get; private set; }
        public Guid HotelServiceId { get; private set; }
        public Guid HotelId { get; private set; } // Composite FK — same hotel

        public string ServiceName { get; private set; } = null!;
        public decimal PriceAtBooking { get; private set; }

        public int Quantity { get; private set; }
        public bool IsAddOn { get; private set; } // true = added after initial booking
        public DateTimeOffset AddedAtUtc { get; private set; }

        public Booking Booking { get; private set; } = null!;
        public HotelService HotelService { get; private set; } = null!;

        public void UpdateQuantity(int quantity)
        {
            Quantity = quantity;
        }

        public void MarkAsAddOn()
        {
            IsAddOn = true;
            AddedAtUtc = DateTimeOffset.UtcNow;
        }
    }

}
