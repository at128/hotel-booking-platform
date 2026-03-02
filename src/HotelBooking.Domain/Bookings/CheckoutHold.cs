using HotelBooking.Domain.Common;
using HotelBooking.Domain.Hotels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Domain.Bookings
{
    public class CheckoutHold : Entity
    {
        private CheckoutHold() { }

        public CheckoutHold(
            Guid id,
            Guid userId,
            Guid hotelId,
            Guid hotelRoomTypeId,
            DateOnly checkIn,
            DateOnly checkOut,
            int quantity,
            DateTimeOffset expiresAtUtc)
            : base(id)
        {
            UserId = userId;
            HotelId = hotelId;
            HotelRoomTypeId = hotelRoomTypeId;
            CheckIn = checkIn;
            CheckOut = checkOut;
            Quantity = quantity;
            ExpiresAtUtc = expiresAtUtc;
            CreatedAtUtc = DateTimeOffset.UtcNow;
        }

        public Guid UserId { get; private set; }
        public Guid HotelId { get; private set; }
        public Guid HotelRoomTypeId { get; private set; }
        public DateOnly CheckIn { get; private set; }
        public DateOnly CheckOut { get; private set; }
        public int Quantity { get; private set; }
        public DateTimeOffset ExpiresAtUtc { get; private set; }
        public bool IsReleased { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; }

        public HotelRoomType HotelRoomType { get; private set; } = null!;

        public bool IsExpired() => DateTimeOffset.UtcNow >= ExpiresAtUtc;

        public void Release()
        {
            IsReleased = true;
        }
    }

}
