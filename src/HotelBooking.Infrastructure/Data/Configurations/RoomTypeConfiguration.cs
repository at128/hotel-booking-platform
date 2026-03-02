using HotelBooking.Domain.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Infrastructure.Data.Configurations
{
    public class RoomTypeConfiguration : IEntityTypeConfiguration<RoomType>
    {
        public void Configure(EntityTypeBuilder<RoomType> builder)
        {
            builder.ToTable("room_types");

            builder.HasKey(rt => rt.Id);

            builder.Property(rt => rt.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(rt => rt.Description)
                .HasMaxLength(500);

            builder.HasIndex(rt => rt.Name)
                .IsUnique();

            builder.HasQueryFilter(rt => rt.DeletedAtUtc == null);
        }
    }

}
