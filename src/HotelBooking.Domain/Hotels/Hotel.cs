using HotelBooking.Domain.Common;
using HotelBooking.Domain.Rooms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Domain.Hotels
{
    public class Hotel : AuditableEntity, ISoftDeletable
    {
        private Hotel() { }

        public Hotel(
            Guid id,
            Guid cityId,
            string name,
            string owner,
            string address,
            short starRating,
            string? description = null,
            decimal? latitude = null,
            decimal? longitude = null,
            string checkInTime = "14:00",
            string checkOutTime = "11:00")
            : base(id)
        {
            CityId = cityId;
            Name = name;
            Owner = owner;
            Address = address;
            StarRating = starRating;
            Description = description;
            Latitude = latitude;
            Longitude = longitude;
            CheckInTime = checkInTime;
            CheckOutTime = checkOutTime;
        }

        public Guid CityId { get; private set; }
        public string Name { get; private set; } = null!;
        public string Owner { get; private set; } = null!;
        public string? Description { get; private set; }
        public string Address { get; private set; } = null!;
        public decimal? Latitude { get; private set; }
        public decimal? Longitude { get; private set; }
        public short StarRating { get; private set; }
        public string CheckInTime { get; private set; } = "14:00";
        public string CheckOutTime { get; private set; } = "11:00";

        public decimal? MinPricePerNight { get; private set; }
        public double AverageRating { get; private set; }
        public int ReviewCount { get; private set; }
        public string? ThumbnailUrl { get; private set; }

        public DateTimeOffset? DeletedAtUtc { get; set; }

        public City City { get; private set; } = null!;
        public ICollection<HotelRoomType> HotelRoomTypes { get; private set; } = [];
        public ICollection<HotelService> HotelServices { get; private set; } = [];
        public ICollection<Room> Rooms { get; private set; } = [];

        public void Update(
            string name,
            string owner,
            string address,
            short starRating,
            string? description,
            decimal? latitude,
            decimal? longitude,
            string checkInTime,
            string checkOutTime)
        {
            Name = name;
            Owner = owner;
            Address = address;
            StarRating = starRating;
            Description = description;
            Latitude = latitude;
            Longitude = longitude;
            CheckInTime = checkInTime;
            CheckOutTime = checkOutTime;
        }

        public void UpdatePriceSummary(decimal minPrice)
        {
            MinPricePerNight = minPrice;
        }

        public void UpdateReviewSummary(double averageRating, int reviewCount)
        {
            AverageRating = averageRating;
            ReviewCount = reviewCount;
        }

        public void SetThumbnail(string? url)
        {
            ThumbnailUrl = url;
        }
    }

}
