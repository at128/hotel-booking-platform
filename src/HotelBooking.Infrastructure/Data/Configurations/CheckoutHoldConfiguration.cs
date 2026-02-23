using HotelBooking.Domain.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Data.Configurations;

public sealed class CheckoutHoldConfiguration : IEntityTypeConfiguration<CheckoutHold>
{
    public void Configure(EntityTypeBuilder<CheckoutHold> builder)
    {
        builder.ToTable("checkout_holds");

        builder.HasKey(ch => ch.Id);

        builder.Property(ch => ch.UserId).IsRequired();
        builder.Property(ch => ch.HotelId).IsRequired();
        builder.Property(ch => ch.HotelRoomTypeId).IsRequired();

        builder.Property(ch => ch.CheckIn)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(ch => ch.CheckOut)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(ch => ch.Quantity)
            .IsRequired();

        builder.Property(ch => ch.ExpiresAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.Property(ch => ch.IsReleased)
            .IsRequired();

        builder.Property(ch => ch.CreatedAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.HasOne(ch => ch.HotelRoomType)
            .WithMany()
            .HasForeignKey(ch => new { ch.HotelRoomTypeId, ch.HotelId })
            .HasPrincipalKey(hrt => new { hrt.Id, hrt.HotelId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ch => new { ch.HotelId, ch.IsReleased, ch.ExpiresAtUtc });
        builder.HasIndex(ch => new { ch.HotelId, ch.ExpiresAtUtc, ch.IsReleased });
        builder.HasIndex(ch => new { ch.HotelId, ch.HotelRoomTypeId, ch.CheckIn, ch.CheckOut });
    }
}