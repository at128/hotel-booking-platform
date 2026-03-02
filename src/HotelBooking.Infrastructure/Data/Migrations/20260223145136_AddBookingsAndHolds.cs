using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingsAndHolds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_hotel_visits_user_recent",
                table: "hotel_visits",
                newName: "IX_HotelVisit_User_VisitedAt");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_rooms_Id_HotelId",
                table: "rooms",
                columns: new[] { "Id", "HotelId" });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HotelAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CheckIn = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckOut = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bookings_hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "checkout_holds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelRoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckIn = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckOut = table.Column<DateOnly>(type: "date", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsReleased = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checkout_holds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_checkout_holds_hotel_room_types_HotelRoomTypeId_HotelId",
                        columns: x => new { x.HotelRoomTypeId, x.HotelId },
                        principalTable: "hotel_room_types",
                        principalColumns: new[] { "Id", "HotelId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "booking_rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelRoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RoomNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PricePerNight = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_booking_rooms_bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_booking_rooms_hotel_room_types_HotelRoomTypeId_HotelId",
                        columns: x => new { x.HotelRoomTypeId, x.HotelId },
                        principalTable: "hotel_room_types",
                        principalColumns: new[] { "Id", "HotelId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_rooms_rooms_RoomId_HotelId",
                        columns: x => new { x.RoomId, x.HotelId },
                        principalTable: "rooms",
                        principalColumns: new[] { "Id", "HotelId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingService",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PriceAtBooking = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    IsAddOn = table.Column<bool>(type: "bit", nullable: false),
                    AddedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingService", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingService_bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingService_hotel_services_HotelServiceId",
                        column: x => x.HotelServiceId,
                        principalTable: "hotel_services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cancellation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RefundPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RefundStatus = table.Column<int>(type: "int", nullable: false),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cancellation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cancellation_bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TransactionRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProviderSessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProviderResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PaidAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payment_bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_rooms_BookingId_HotelRoomTypeId",
                table: "booking_rooms",
                columns: new[] { "BookingId", "HotelRoomTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_rooms_HotelId_HotelRoomTypeId",
                table: "booking_rooms",
                columns: new[] { "HotelId", "HotelRoomTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_rooms_HotelId_RoomId",
                table: "booking_rooms",
                columns: new[] { "HotelId", "RoomId" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_rooms_HotelRoomTypeId_HotelId",
                table: "booking_rooms",
                columns: new[] { "HotelRoomTypeId", "HotelId" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_rooms_RoomId_HotelId",
                table: "booking_rooms",
                columns: new[] { "RoomId", "HotelId" });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_BookingNumber",
                table: "bookings",
                column: "BookingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_HotelId_CheckIn_CheckOut_Status",
                table: "bookings",
                columns: new[] { "HotelId", "CheckIn", "CheckOut", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingService_BookingId",
                table: "BookingService",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingService_HotelServiceId",
                table: "BookingService",
                column: "HotelServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Cancellation_BookingId",
                table: "Cancellation",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_checkout_holds_HotelId_ExpiresAtUtc_IsReleased",
                table: "checkout_holds",
                columns: new[] { "HotelId", "ExpiresAtUtc", "IsReleased" });

            migrationBuilder.CreateIndex(
                name: "IX_checkout_holds_HotelId_HotelRoomTypeId_CheckIn_CheckOut",
                table: "checkout_holds",
                columns: new[] { "HotelId", "HotelRoomTypeId", "CheckIn", "CheckOut" });

            migrationBuilder.CreateIndex(
                name: "IX_checkout_holds_HotelRoomTypeId_HotelId",
                table: "checkout_holds",
                columns: new[] { "HotelRoomTypeId", "HotelId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payment_BookingId",
                table: "Payment",
                column: "BookingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_rooms");

            migrationBuilder.DropTable(
                name: "BookingService");

            migrationBuilder.DropTable(
                name: "Cancellation");

            migrationBuilder.DropTable(
                name: "checkout_holds");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_rooms_Id_HotelId",
                table: "rooms");

            migrationBuilder.RenameIndex(
                name: "IX_HotelVisit_User_VisitedAt",
                table: "hotel_visits",
                newName: "IX_hotel_visits_user_recent");
        }
    }
}
