using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixBookingRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cancellations_bookings_BookingId1",
                table: "cancellations");

            migrationBuilder.DropForeignKey(
                name: "FK_featured_deals_hotel_room_types_HotelRoomTypeId",
                table: "featured_deals");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_bookings_BookingId1",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_BookingId1",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_featured_deals_HotelId",
                table: "featured_deals");

            migrationBuilder.DropIndex(
                name: "IX_featured_deals_HotelRoomTypeId",
                table: "featured_deals");

            migrationBuilder.DropIndex(
                name: "IX_cancellations_BookingId1",
                table: "cancellations");

            migrationBuilder.DropColumn(
                name: "BookingId1",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "BookingId1",
                table: "cancellations");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_hotel_room_types_HotelId_Id",
                table: "hotel_room_types",
                columns: new[] { "HotelId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_featured_deals_HotelId_HotelRoomTypeId",
                table: "featured_deals",
                columns: new[] { "HotelId", "HotelRoomTypeId" });

            migrationBuilder.AddForeignKey(
                name: "FK_featured_deals_hotel_room_types_HotelId_HotelRoomTypeId",
                table: "featured_deals",
                columns: new[] { "HotelId", "HotelRoomTypeId" },
                principalTable: "hotel_room_types",
                principalColumns: new[] { "HotelId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_featured_deals_hotel_room_types_HotelId_HotelRoomTypeId",
                table: "featured_deals");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_hotel_room_types_HotelId_Id",
                table: "hotel_room_types");

            migrationBuilder.DropIndex(
                name: "IX_featured_deals_HotelId_HotelRoomTypeId",
                table: "featured_deals");

            migrationBuilder.AddColumn<Guid>(
                name: "BookingId1",
                table: "payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BookingId1",
                table: "cancellations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_BookingId1",
                table: "payments",
                column: "BookingId1");

            migrationBuilder.CreateIndex(
                name: "IX_featured_deals_HotelId",
                table: "featured_deals",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_featured_deals_HotelRoomTypeId",
                table: "featured_deals",
                column: "HotelRoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_cancellations_BookingId1",
                table: "cancellations",
                column: "BookingId1",
                unique: true,
                filter: "[BookingId1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_cancellations_bookings_BookingId1",
                table: "cancellations",
                column: "BookingId1",
                principalTable: "bookings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_featured_deals_hotel_room_types_HotelRoomTypeId",
                table: "featured_deals",
                column: "HotelRoomTypeId",
                principalTable: "hotel_room_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_bookings_BookingId1",
                table: "payments",
                column: "BookingId1",
                principalTable: "bookings",
                principalColumn: "Id");
        }
    }
}
