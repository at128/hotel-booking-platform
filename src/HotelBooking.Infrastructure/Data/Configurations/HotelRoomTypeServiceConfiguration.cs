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
    public class HotelRoomTypeServiceConfiguration : IEntityTypeConfiguration<HotelRoomTypeService>
    {
        public void Configure(EntityTypeBuilder<HotelRoomTypeService> builder)
        {
            builder.ToTable("hotel_room_type_services");

            builder.HasKey(x => x.Id);

            // Composite FK — ensures HotelRoomType and HotelService belong to same hotel
            builder.HasOne(x => x.HotelRoomType)
                .WithMany(hrt => hrt.HotelRoomTypeServices)
                .HasForeignKey(x => new { x.HotelRoomTypeId, x.HotelId })
                .HasPrincipalKey(hrt => new { hrt.Id, hrt.HotelId })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.HotelService)
                .WithMany(hs => hs.HotelRoomTypeServices)
                .HasForeignKey(x => new { x.HotelServiceId, x.HotelId })
                .HasPrincipalKey(hs => new { hs.Id, hs.HotelId })
                .OnDelete(DeleteBehavior.Restrict);

            // Constraints — prevent duplicate assignments
            builder.HasIndex(x => new { x.HotelRoomTypeId, x.HotelServiceId })
                .IsUnique();
        }
    }

}
