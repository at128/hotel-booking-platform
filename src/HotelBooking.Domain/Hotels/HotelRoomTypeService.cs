using HotelBooking.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Domain.Hotels
{
    public class HotelRoomTypeService : Entity
    {
        private HotelRoomTypeService() { }

        public HotelRoomTypeService(
            Guid id,
            Guid hotelRoomTypeId,
            Guid hotelServiceId,
            Guid hotelId)
            : base(id)
        {
            HotelRoomTypeId = hotelRoomTypeId;
            HotelServiceId = hotelServiceId;
            HotelId = hotelId;
        }

        public Guid HotelRoomTypeId { get; private set; }
        public Guid HotelServiceId { get; private set; }
        public Guid HotelId { get; private set; } // Shared FK — ensures same hotel

        public HotelRoomType HotelRoomType { get; private set; } = null!;
        public HotelService HotelService { get; private set; } = null!;
    }

}
