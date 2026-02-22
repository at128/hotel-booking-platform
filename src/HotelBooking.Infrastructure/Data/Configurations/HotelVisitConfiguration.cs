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
    public class HotelVisitConfiguration : IEntityTypeConfiguration<HotelVisit>
    {
        public void Configure(EntityTypeBuilder<HotelVisit> builder)
        {
            builder.ToTable("hotel_visits");

            builder.HasKey(hv => hv.Id);

            builder.HasOne(hv => hv.Hotel)
                .WithMany()
                .HasForeignKey(hv => hv.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(hv => new { hv.UserId, hv.VisitedAtUtc })
                .IsDescending(false, true)
                .HasDatabaseName("IX_hotel_visits_user_recent");
            builder.HasIndex(hv => new { hv.UserId, hv.VisitedAtUtc })
    .IsDescending(false, true)
    .HasDatabaseName("IX_HotelVisit_User_VisitedAt");
            // Prevent duplicate: update visit time instead of inserting new
            builder.HasIndex(hv => new { hv.UserId, hv.HotelId })
                .IsUnique();
        }
    }

}
