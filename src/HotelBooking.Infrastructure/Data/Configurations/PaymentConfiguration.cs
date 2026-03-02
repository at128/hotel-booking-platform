using HotelBooking.Domain.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Data.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.Method)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.TransactionRef)
            .HasMaxLength(200);

        builder.Property(x => x.ProviderSessionId)
            .HasMaxLength(200);

        builder.Property(x => x.ProviderResponseJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.Property(x => x.PaidAtUtc)
            .HasColumnType("datetimeoffset");

        builder.HasOne(x => x.Booking)
            .WithMany(b => b.Payments)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.BookingId);
        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => x.TransactionRef)
            .IsUnique()
            .HasFilter("[TransactionRef] IS NOT NULL");

        builder.HasIndex(x => x.ProviderSessionId)
            .HasFilter("[ProviderSessionId] IS NOT NULL");
    }
}