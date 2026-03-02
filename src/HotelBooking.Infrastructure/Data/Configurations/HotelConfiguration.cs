using HotelBooking.Domain.Hotels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace HotelBooking.Infrastructure.Data.Configurations
{
    public class HotelConfiguration : IEntityTypeConfiguration<Hotel>
    {
        public void Configure(EntityTypeBuilder<Hotel> builder)
        {
            builder.ToTable("hotels");

            builder.HasKey(h => h.Id);

            builder.Property(h => h.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(h => h.Owner)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(h => h.Description)
                .HasMaxLength(2000);

            builder.Property(h => h.Address)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(h => h.Latitude)
                .HasPrecision(9, 6);

            builder.Property(h => h.Longitude)
                .HasPrecision(9, 6);

            builder.Property(h => h.CheckInTime)
                .HasMaxLength(5)
                .IsRequired();

            builder.Property(h => h.CheckOutTime)
                .HasMaxLength(5)
                .IsRequired();

            builder.Property(h => h.MinPricePerNight)
                .HasPrecision(10, 2);

            builder.Property(h => h.ThumbnailUrl)
                .HasMaxLength(500);

            builder.HasOne(h => h.City)
                .WithMany(c => c.Hotels)
                .HasForeignKey(h => h.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(h => new { h.Name, h.CityId })
                .IsUnique();

            builder.HasIndex(h => new { h.Id, h.CityId })
                .IsUnique();

            builder.HasIndex(h => new { h.CityId, h.StarRating, h.MinPricePerNight })
                .HasFilter("[DeletedAtUtc] IS NULL");

            builder.HasIndex(h => new { h.MinPricePerNight, h.Id })
                .HasFilter("[DeletedAtUtc] IS NULL");

            builder.HasQueryFilter(h => h.DeletedAtUtc == null);
        }
    }

}
