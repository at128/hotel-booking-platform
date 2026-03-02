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
    public class CityConfiguration : IEntityTypeConfiguration<City>
    {
        public void Configure(EntityTypeBuilder<City> builder)
        {
            builder.ToTable("cities");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(c => c.Country)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(c => c.PostOffice)
                .HasMaxLength(20);

            builder.HasIndex(c => new { c.Name, c.Country })
                .IsUnique();

            builder.HasQueryFilter(c => c.DeletedAtUtc == null);
        }
    }
}
