using HotelBooking.Domain.Common;
using HotelBooking.Domain.Hotels.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Domain.Hotels
{
    public class Image : AuditableEntity
    {
        private Image() { }

        public Image(
            Guid id,
            ImageType entityType,
            Guid entityId,
            string url,
            string? caption = null,
            int sortOrder = 0)
            : base(id)
        {
            EntityType = entityType;
            EntityId = entityId;
            Url = url;
            Caption = caption;
            SortOrder = sortOrder;
        }

        public ImageType EntityType { get; private set; }  
        public Guid EntityId { get; private set; }        
        public string Url { get; private set; } = null!;
        public string? Caption { get; private set; }
        public int SortOrder { get; private set; }

        public void Update(string? caption, int sortOrder)
        {
            Caption = caption;
            SortOrder = sortOrder;
        }

        public void UpdateUrl(string url)
        {
            Url = url;
        }
    }
}
