using HotelBooking.Domain.Common;
using HotelBooking.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Domain.Hotels
{
    public class HotelService : Entity
    {
        private HotelService() { }

        public HotelService(
            Guid id,
            Guid hotelId,
            Guid serviceId,
            decimal price = 0,
            bool isFree = true)
            : base(id)
        {
            HotelId = hotelId;
            ServiceId = serviceId;
            Price = price;
            IsFree = isFree;
        }

        public Guid HotelId { get; private set; }
        public Guid ServiceId { get; private set; }
        public decimal Price { get; private set; }
        public bool IsFree { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }

        public Hotel Hotel { get; private set; } = null!;
        public Service Service { get; private set; } = null!;
        public ICollection<HotelRoomTypeService> HotelRoomTypeServices { get; private set; } = [];

        public void Update(decimal price, bool isFree)
        {
            Price = price;
            IsFree = isFree;
        }
    }

}
