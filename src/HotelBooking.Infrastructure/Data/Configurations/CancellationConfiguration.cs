using HotelBooking.Domain.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Data.Configurations;

public sealed class CancellationConfiguration : IEntityTypeConfiguration<Cancellation>
{
    public void Configure(EntityTypeBuilder<Cancellation> builder)
    {
        builder.ToTable("cancellations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason)
            .HasMaxLength(1000);

        builder.Property(x => x.RefundAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        // Stored as ratio between 0 and 1 (e.g. 0.30 = 30%)
        builder.Property(x => x.RefundPercentage)
            .HasColumnType("decimal(5,4)")
            .IsRequired();

        builder.Property(x => x.RefundStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CancelledAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.HasOne(x => x.Booking)
            .WithOne()
            .HasForeignKey<Cancellation>(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        // One cancellation per booking
        builder.HasIndex(x => x.BookingId)
            .IsUnique();
    }
}