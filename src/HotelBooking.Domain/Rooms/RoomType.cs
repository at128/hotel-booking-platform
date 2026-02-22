using HotelBooking.Domain.Common;
using HotelBooking.Domain.Hotels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Domain.Rooms
{
    public class RoomType : AuditableEntity, ISoftDeletable
    {
        private RoomType() { }

        public RoomType(Guid id, string name, string? description = null)
            : base(id)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; private set; } = null!;
        public string? Description { get; private set; }
        public DateTimeOffset? DeletedAtUtc { get; set; }

        public ICollection<HotelRoomType> HotelRoomTypes { get; private set; } = [];

        public void Update(string name, string? description)
        {
            Name = name;
            Description = description;
        }
    }
}
