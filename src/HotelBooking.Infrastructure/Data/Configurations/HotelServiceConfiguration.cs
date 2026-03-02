using HotelBooking.Domain.Hotels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Infrastructure.Data.Configurations
{
    public class HotelServiceConfiguration : IEntityTypeConfiguration<HotelService>
    {
        public void Configure(EntityTypeBuilder<HotelService> builder)
        {
            builder.ToTable("hotel_services");

            builder.HasKey(hs => hs.Id);

            builder.Property(hs => hs.Price)
                .HasPrecision(10, 2)
                .IsRequired();

            builder.HasOne(hs => hs.Hotel)
                .WithMany(h => h.HotelServices)
                .HasForeignKey(hs => hs.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(hs => hs.Service)
                .WithMany(s => s.HotelServices)
                .HasForeignKey(hs => hs.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(hs => new { hs.HotelId, hs.ServiceId })
                .IsUnique();

            // Composite unique — needed for Composite FK from HotelRoomTypeService & BookingService
            builder.HasIndex(hs => new { hs.Id, hs.HotelId })
                .IsUnique();

            builder.HasIndex(hs => hs.ServiceId);
        }
    }

}
