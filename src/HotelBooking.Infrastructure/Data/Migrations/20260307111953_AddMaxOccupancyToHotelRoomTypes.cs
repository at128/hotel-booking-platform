using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxOccupancyToHotelRoomTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rooms_HotelId_RoomNumber",
                table: "rooms");

            migrationBuilder.AlterColumn<decimal>(
                name: "PricePerNight",
                table: "hotel_room_types",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AddColumn<short>(
                name: "MaxOccupancy",
                table: "hotel_room_types",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AlterColumn<string>(
                name: "UserEmail",
                table: "bookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.CreateIndex(
                name: "IX_rooms_HotelId_RoomNumber",
                table: "rooms",
                columns: new[] { "HotelId", "RoomNumber" },
                unique: true,
                filter: "[DeletedAtUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rooms_HotelId_RoomNumber",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "MaxOccupancy",
                table: "hotel_room_types");

            migrationBuilder.AlterColumn<decimal>(
                name: "PricePerNight",
                table: "hotel_room_types",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "UserEmail",
                table: "bookings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_rooms_HotelId_RoomNumber",
                table: "rooms",
                columns: new[] { "HotelId", "RoomNumber" },
                unique: true);
        }
    }
}
