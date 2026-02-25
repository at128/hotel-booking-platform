using HotelBooking.Domain.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Data.Configurations;

public sealed class BookingRoomConfiguration : IEntityTypeConfiguration<BookingRoom>
{
    public void Configure(EntityTypeBuilder<BookingRoom> builder)
    {
        builder.ToTable("booking_rooms");

        builder.HasKey(br => br.Id);

        builder.Property(br => br.PricePerNight)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(br => br.RoomTypeName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(br => br.RoomNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne(br => br.Booking)
            .WithMany(b => b.BookingRooms)
            .HasForeignKey(br => br.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(br => br.Room)
            .WithMany()
            .HasForeignKey(br => new { br.RoomId, br.HotelId })
            .HasPrincipalKey(r => new { r.Id, r.HotelId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(br => br.HotelRoomType)
            .WithMany()
            .HasForeignKey(br => new { br.HotelRoomTypeId, br.HotelId })
            .HasPrincipalKey(hrt => new { hrt.Id, hrt.HotelId })
            .OnDelete(DeleteBehavior.Restrict);



        builder.HasIndex(br => new { br.BookingId, br.HotelRoomTypeId });
        builder.HasIndex(br => new { br.HotelId, br.HotelRoomTypeId });
        builder.HasIndex(br => new { br.HotelId, br.RoomId });
    }
}