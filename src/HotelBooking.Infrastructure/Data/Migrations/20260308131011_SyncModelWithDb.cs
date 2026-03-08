using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelWithDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_BookingId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_hotel_visits_HotelId",
                table: "hotel_visits");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BookingId",
                table: "Reviews",
                column: "BookingId",
                unique: true,
                filter: "[DeletedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_hotels_CityId",
                table: "hotels",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_hotels_DeletedAtUtc",
                table: "hotels",
                column: "DeletedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_hotels_MinPricePerNight",
                table: "hotels",
                column: "MinPricePerNight");

            migrationBuilder.CreateIndex(
                name: "IX_hotels_StarRating",
                table: "hotels",
                column: "StarRating");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_visits_HotelId_VisitedAtUtc",
                table: "hotel_visits",
                columns: new[] { "HotelId", "VisitedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_hotel_visits_UserId",
                table: "hotel_visits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_CheckIn_CheckOut",
                table: "bookings",
                columns: new[] { "CheckIn", "CheckOut" });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_HotelId",
                table: "bookings",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_Status",
                table: "bookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_UserId",
                table: "bookings",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_BookingId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_hotels_CityId",
                table: "hotels");

            migrationBuilder.DropIndex(
                name: "IX_hotels_DeletedAtUtc",
                table: "hotels");

            migrationBuilder.DropIndex(
                name: "IX_hotels_MinPricePerNight",
                table: "hotels");

            migrationBuilder.DropIndex(
                name: "IX_hotels_StarRating",
                table: "hotels");

            migrationBuilder.DropIndex(
                name: "IX_hotel_visits_HotelId_VisitedAtUtc",
                table: "hotel_visits");

            migrationBuilder.DropIndex(
                name: "IX_hotel_visits_UserId",
                table: "hotel_visits");

            migrationBuilder.DropIndex(
                name: "IX_bookings_CheckIn_CheckOut",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_HotelId",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_Status",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_UserId",
                table: "bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BookingId",
                table: "Reviews",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_visits_HotelId",
                table: "hotel_visits",
                column: "HotelId");
        }
    }
}
