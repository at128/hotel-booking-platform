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
    public class ImageConfiguration : IEntityTypeConfiguration<Image>
    {
        public void Configure(EntityTypeBuilder<Image> builder)
        {
            builder.ToTable("images");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.EntityType)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(i => i.Url)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(i => i.Caption)
                .HasMaxLength(200);

            builder.HasIndex(i => new { i.EntityType, i.EntityId, i.SortOrder })
                .HasDatabaseName("IX_images_entity_sort");
        }
    }

}
