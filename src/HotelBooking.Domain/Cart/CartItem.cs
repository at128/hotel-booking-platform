using HotelBooking.Domain.Common;
using HotelBooking.Domain.Hotels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Domain.Cart
{
    public class CartItem : Entity
    {
        private CartItem() { }

        public CartItem(
            Guid id,
            Guid userId,
            Guid hotelId,
            Guid hotelRoomTypeId,
            DateOnly checkIn,
            DateOnly checkOut,
            int quantity)
            : base(id)
        {
            UserId = userId;
            HotelId = hotelId;
            HotelRoomTypeId = hotelRoomTypeId;
            CheckIn = checkIn;
            CheckOut = checkOut;
            Quantity = quantity;
        }

        public Guid UserId { get; private set; }
        public Guid HotelId { get; private set; }
        public Guid HotelRoomTypeId { get; private set; }
        public DateOnly CheckIn { get; private set; }
        public DateOnly CheckOut { get; private set; }
        public int Quantity { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; }

        public Hotel Hotel { get; private set; } = null!;
        public HotelRoomType HotelRoomType { get; private set; } = null!;

        public void UpdateQuantity(int quantity)
        {
            Quantity = quantity;
        }

        public void UpdateDates(DateOnly checkIn, DateOnly checkOut)
        {
            CheckIn = checkIn;
            CheckOut = checkOut;
        }
    }

}
