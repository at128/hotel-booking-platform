using HotelBooking.Domain.Hotels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Infrastructure.Data.Configurations
{
    public class FeaturedDealConfiguration : IEntityTypeConfiguration<FeaturedDeal>
    {
        public void Configure(EntityTypeBuilder<FeaturedDeal> builder)
        {
            builder.ToTable("featured_deals");

            builder.HasKey(fd => fd.Id);

            builder.Property(fd => fd.OriginalPrice)
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(fd => fd.DiscountedPrice)
                .HasPrecision(10, 2)
                .IsRequired();

            builder.HasOne(fd => fd.Hotel)
                .WithMany()
                .HasForeignKey(fd => fd.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(fd => fd.HotelRoomType)
                .WithMany()
                .HasForeignKey(fd => new { fd.HotelRoomTypeId, fd.HotelId })
                .HasPrincipalKey(hrt => new { hrt.Id, hrt.HotelId })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(fd => fd.DisplayOrder);
        }
    }

}
