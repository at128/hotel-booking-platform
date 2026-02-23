
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Hotels;

namespace HotelBooking.Domain.Reviews
{
    public class Review : AuditableEntity, ISoftDeletable
    {
        private Review() { }

        public Review(
            Guid id,
            Guid userId,
            Guid hotelId,
            Guid bookingId,
            short rating,
            string? title = null,
            string? comment = null)
            : base(id)
        {
            UserId = userId;
            HotelId = hotelId;
            BookingId = bookingId;
            Rating = rating;
            Title = title;
            Comment = comment;
        }

        public Guid UserId { get; private set; }
        public Guid HotelId { get; private set; }
        public Guid BookingId { get; private set; } 
        public short Rating { get; private set; }  
        public string? Title { get; private set; }
        public string? Comment { get; private set; }


        public Hotel Hotel { get; private set; } = null!;
        public Booking Booking { get; private set; } = null!;
        public DateTimeOffset? DeletedAtUtc { get; set; }

        // Methods
        public void Update(short rating, string? title, string? comment)
        {
            Rating = rating;
            Title = title;
            Comment = comment;
        }
    }

}
