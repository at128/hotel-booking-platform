using HotelBooking.Domain.Common;
using HotelBooking.Domain.Hotels;

namespace HotelBooking.Domain.Services
{
    public class Service : Entity,ISoftDeletable
    {
        private Service() { }

        public Service(Guid id, string name, string? description = null)
            : base(id)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; private set; } = null!;
        public string? Description { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
        

        public ICollection<HotelService> HotelServices { get; private set; } = [];
        public DateTimeOffset? DeletedAtUtc { get; set; }

        public void Update(string name, string? description)
        {
            Name = name;
            Description = description;
        }
    }

}
