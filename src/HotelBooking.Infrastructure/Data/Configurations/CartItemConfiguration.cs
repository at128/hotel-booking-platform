using HotelBooking.Domain.Cart;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Data.Configurations;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId).IsRequired();
        builder.Property(c => c.HotelId).IsRequired();
        builder.Property(c => c.HotelRoomTypeId).IsRequired();
        builder.Property(c => c.CheckIn).IsRequired();
        builder.Property(c => c.CheckOut).IsRequired();
        builder.Property(c => c.Quantity).IsRequired();
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        // Composite unique index: one item per user per room type per date range
        builder.HasIndex(c => new { c.UserId, c.HotelRoomTypeId, c.CheckIn, c.CheckOut })
            .IsUnique()
            .HasDatabaseName("IX_CartItems_User_RoomType_Dates");

        // Index for fast "get user's cart" query
        builder.HasIndex(c => c.UserId)
            .HasDatabaseName("IX_CartItems_UserId");

        builder.HasOne(c => c.Hotel)
            .WithMany()
            .HasForeignKey(c => c.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.HotelRoomType)
            .WithMany()
            .HasForeignKey(c => c.HotelRoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}