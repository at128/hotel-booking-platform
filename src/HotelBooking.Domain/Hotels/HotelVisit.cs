using HotelBooking.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Domain.Hotels
{
    public class HotelVisit : Entity
    {
        private HotelVisit() { }

        public HotelVisit(Guid id, Guid userId, Guid hotelId)
            : base(id)
        {
            UserId = userId;
            HotelId = hotelId;
        }

        public Guid UserId { get; private set; }
        public Guid HotelId { get; private set; }
        public DateTimeOffset VisitedAtUtc { get; private set; }

        public Hotel Hotel { get; private set; } = null!;

        public void UpdateVisitTime()
        {
            VisitedAtUtc = DateTimeOffset.UtcNow;
        }
    }

}
