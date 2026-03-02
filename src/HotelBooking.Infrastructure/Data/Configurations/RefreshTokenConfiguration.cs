using HotelBooking.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Data.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.TokenHash)
            .HasMaxLength(128) 
            .IsRequired();

        builder.Property(t => t.Family)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(t => t.ReplacedByTokenHash)
            .HasMaxLength(128);

        builder.Property(t => t.DeviceInfo)
            .HasMaxLength(200);

        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.HasIndex(t => new { t.UserId, t.Family });

        builder.HasIndex(t => t.ExpiresAt);

        builder.HasIndex(t => new { t.Family, t.IsRevoked, t.IsUsed });
    }
}