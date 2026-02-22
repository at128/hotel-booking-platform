using HotelBooking.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Domain.Hotels
{
    public class FeaturedDeal : AuditableEntity
    {
        private FeaturedDeal() { }

        public FeaturedDeal(
            Guid id,
            Guid hotelId,
            Guid hotelRoomTypeId,
            decimal originalPrice,
            decimal discountedPrice,
            int displayOrder = 0,
            DateTimeOffset? startsAtUtc = null,
            DateTimeOffset? endsAtUtc = null)
            : base(id)
        {
            HotelId = hotelId;
            HotelRoomTypeId = hotelRoomTypeId;
            OriginalPrice = originalPrice;
            DiscountedPrice = discountedPrice;
            DisplayOrder = displayOrder;
            StartsAtUtc = startsAtUtc;
            EndsAtUtc = endsAtUtc;
        }

        public Guid HotelId { get; private set; }
        public Guid HotelRoomTypeId { get; private set; }
        public decimal OriginalPrice { get; private set; }
        public decimal DiscountedPrice { get; private set; }
        public int DisplayOrder { get; private set; }
        public DateTimeOffset? StartsAtUtc { get; private set; }
        public DateTimeOffset? EndsAtUtc { get; private set; }

        public Hotel Hotel { get; private set; } = null!;
        public HotelRoomType HotelRoomType { get; private set; } = null!;

        public bool IsActive()
        {
            var now = DateTimeOffset.UtcNow;
            return (StartsAtUtc == null || now >= StartsAtUtc)
                && (EndsAtUtc == null || now <= EndsAtUtc);
        }

        public void Update(
            decimal originalPrice,
            decimal discountedPrice,
            int displayOrder,
            DateTimeOffset? startsAtUtc,
            DateTimeOffset? endsAtUtc)
        {
            OriginalPrice = originalPrice;
            DiscountedPrice = discountedPrice;
            DisplayOrder = displayOrder;
            StartsAtUtc = startsAtUtc;
            EndsAtUtc = endsAtUtc;
        }
    }

}
