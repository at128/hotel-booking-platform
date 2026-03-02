using HotelBooking.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Domain.Hotels
{
    public class City : AuditableEntity, ISoftDeletable
    {
        private City() { }

        public City(Guid id, string name, string country, string? postOffice)
            : base(id)
        {
            Name = name;
            Country = country;
            PostOffice = postOffice;
        }

        public string Name { get; private set; } = null!;
        public string Country { get; private set; } = null!;
        public string? PostOffice { get; private set; }
        public DateTimeOffset? DeletedAtUtc { get; set; }

        public ICollection<Hotel> Hotels { get; private set; } = [];


        public void Update(string name, string country, string? postOffice)
        {
            Name = name;
            Country = country;
            PostOffice = postOffice;
        }
    }

}
