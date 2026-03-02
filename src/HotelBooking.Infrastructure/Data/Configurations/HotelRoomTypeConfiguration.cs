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
    public class HotelRoomTypeConfiguration : IEntityTypeConfiguration<HotelRoomType>
    {
        public void Configure(EntityTypeBuilder<HotelRoomType> builder)
        {
            builder.ToTable("hotel_room_types");

            builder.HasKey(hrt => hrt.Id);

            builder.Property(hrt => hrt.PricePerNight)
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(hrt => hrt.Description)
                .HasMaxLength(500);

            builder.HasOne(hrt => hrt.Hotel)
                .WithMany(h => h.HotelRoomTypes)
                .HasForeignKey(hrt => hrt.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(hrt => hrt.RoomType)
                .WithMany(rt => rt.HotelRoomTypes)
                .HasForeignKey(hrt => hrt.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(hrt => new { hrt.HotelId, hrt.RoomTypeId })
                .IsUnique();

            // Composite unique — needed for Composite FK from Room & HotelRoomTypeService
            builder.HasIndex(hrt => new { hrt.Id, hrt.HotelId })
                .IsUnique();

            builder.HasIndex(hrt => hrt.HotelId);
            builder.HasIndex(hrt => hrt.RoomTypeId);

            builder.HasQueryFilter(hrt => hrt.DeletedAtUtc == null);
        }
    }

}
