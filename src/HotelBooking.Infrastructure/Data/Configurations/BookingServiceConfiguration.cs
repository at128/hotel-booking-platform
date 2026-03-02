using HotelBooking.Domain.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Data.Configurations;

public sealed class BookingServiceConfiguration : IEntityTypeConfiguration<BookingService>
{
    public void Configure(EntityTypeBuilder<BookingService> builder)
    {
        builder.ToTable("booking_services");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PriceAtBooking)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.IsAddOn)
            .IsRequired();

        builder.Property(x => x.AddedAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.HasOne(x => x.Booking)
            .WithMany(b => b.BookingServices)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composite FK — ensures the hotel service belongs to the same hotel
        builder.HasOne(x => x.HotelService)
            .WithMany()
            .HasForeignKey(x => new { x.HotelServiceId, x.HotelId })
            .HasPrincipalKey(hs => new { hs.Id, hs.HotelId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BookingId);
        builder.HasIndex(x => new { x.HotelId, x.HotelServiceId });
        builder.HasIndex(x => new { x.BookingId, x.HotelServiceId });
    }
}