using HotelBooking.Domain.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static HotelBooking.Domain.Common.Constants.HotelBookingConstants;

namespace HotelBooking.Infrastructure.Data.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Domain.Bookings.Booking>
{
    public void Configure(EntityTypeBuilder<Domain.Bookings.Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BookingNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(b => b.BookingNumber).IsUnique();

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(b => b.CheckIn)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(b => b.CheckOut)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(b => b.HotelName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.HotelAddress)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(b => b.UserEmail)
                .HasMaxLength(FieldLengths.EmailMaxLength) // = 256
                .IsRequired();

        builder.Property(b => b.TotalAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(b => b.Notes)
            .HasMaxLength(1000);

        builder.HasOne(b => b.Hotel)
            .WithMany()
            .HasForeignKey(b => b.HotelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.BookingRooms)
            .WithOne(br => br.Booking)
            .HasForeignKey(br => br.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => new { b.HotelId, b.CheckIn, b.CheckOut, b.Status });
    }
}