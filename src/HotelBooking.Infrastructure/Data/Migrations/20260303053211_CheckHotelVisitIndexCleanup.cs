using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CheckHotelVisitIndexCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.RenameIndex(
                name: "IX_HotelVisit_User_VisitedAt",
                table: "hotel_visits",
                newName: "IX_hotel_visits_user_recent");

            migrationBuilder.CreateIndex(
                name: "IX_featured_deals_HotelId",
                table: "featured_deals",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_featured_deals_HotelRoomTypeId_HotelId",
                table: "featured_deals",
                columns: new[] { "HotelRoomTypeId", "HotelId" });

            migrationBuilder.AddForeignKey(
                name: "FK_featured_deals_hotel_room_types_HotelRoomTypeId_HotelId",
                table: "featured_deals",
                columns: new[] { "HotelRoomTypeId", "HotelId" },
                principalTable: "hotel_room_types",
                principalColumns: new[] { "Id", "HotelId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_featured_deals_hotel_room_types_HotelRoomTypeId_HotelId",
                table: "featured_deals");

            migrationBuilder.DropIndex(
                name: "IX_featured_deals_HotelId",
                table: "featured_deals");

            migrationBuilder.DropIndex(
                name: "IX_featured_deals_HotelRoomTypeId_HotelId",
                table: "featured_deals");

            migrationBuilder.RenameIndex(
                name: "IX_hotel_visits_user_recent",
                table: "hotel_visits",
                newName: "IX_HotelVisit_User_VisitedAt");

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
    }
}
