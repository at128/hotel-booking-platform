using HotelBooking.Domain.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Data.Configurations
{
    public class RoomConfiguration : IEntityTypeConfiguration<Room>
    {
        public void Configure(EntityTypeBuilder<Room> builder)
        {
            builder.ToTable("rooms");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.RoomNumber)
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(r => r.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            // Relationships — Composite FK ensures room & room type belong to same hotel
            builder.HasOne(r => r.HotelRoomType)
                .WithMany(hrt => hrt.Rooms)
                .HasForeignKey(r => new { r.HotelRoomTypeId, r.HotelId })
                .HasPrincipalKey(hrt => new { hrt.Id, hrt.HotelId })
                .OnDelete(DeleteBehavior.Restrict);

            

            builder.HasOne(r => r.Hotel)
                .WithMany(h => h.Rooms)
                .HasForeignKey(r => r.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => new { r.HotelId, r.RoomNumber })
                .IsUnique();

            // Composite unique — for Composite FK from BookingRoom
            builder.HasIndex(r => new { r.Id, r.HotelId })
                .IsUnique();

            builder.HasIndex(r => new { r.HotelRoomTypeId, r.Status })
                .HasFilter("[DeletedAtUtc] IS NULL");

            builder.HasQueryFilter(r => r.DeletedAtUtc == null);
        }
    }

}
