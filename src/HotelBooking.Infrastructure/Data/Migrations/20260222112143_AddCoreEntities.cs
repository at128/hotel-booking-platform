using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCoreEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostOffice = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "images",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_images", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "room_types",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Owner = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    StarRating = table.Column<short>(type: "smallint", nullable: false),
                    CheckInTime = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    CheckOutTime = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    MinPricePerNight = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    AverageRating = table.Column<double>(type: "float", nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hotels_cities_CityId",
                        column: x => x.CityId,
                        principalTable: "cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hotel_room_types",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PricePerNight = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    AdultCapacity = table.Column<short>(type: "smallint", nullable: false),
                    ChildCapacity = table.Column<short>(type: "smallint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_room_types", x => x.Id);
                    table.UniqueConstraint("AK_hotel_room_types_Id_HotelId", x => new { x.Id, x.HotelId });
                    table.ForeignKey(
                        name: "FK_hotel_room_types_hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hotel_room_types_room_types_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "room_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hotel_services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    IsFree = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_services", x => x.Id);
                    table.UniqueConstraint("AK_hotel_services_Id_HotelId", x => new { x.Id, x.HotelId });
                    table.ForeignKey(
                        name: "FK_hotel_services_hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hotel_services_services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hotel_visits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisitedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_visits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hotel_visits_hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "featured_deals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelRoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    DiscountedPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_featured_deals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_featured_deals_hotel_room_types_HotelRoomTypeId",
                        column: x => x.HotelRoomTypeId,
                        principalTable: "hotel_room_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_featured_deals_hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelRoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Floor = table.Column<short>(type: "smallint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rooms_hotel_room_types_HotelRoomTypeId_HotelId",
                        columns: x => new { x.HotelRoomTypeId, x.HotelId },
                        principalTable: "hotel_room_types",
                        principalColumns: new[] { "Id", "HotelId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rooms_hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hotel_room_type_services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelRoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_room_type_services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hotel_room_type_services_hotel_room_types_HotelRoomTypeId_HotelId",
                        columns: x => new { x.HotelRoomTypeId, x.HotelId },
                        principalTable: "hotel_room_types",
                        principalColumns: new[] { "Id", "HotelId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hotel_room_type_services_hotel_services_HotelServiceId_HotelId",
                        columns: x => new { x.HotelServiceId, x.HotelId },
                        principalTable: "hotel_services",
                        principalColumns: new[] { "Id", "HotelId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cities_Name_Country",
                table: "cities",
                columns: new[] { "Name", "Country" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_featured_deals_DisplayOrder",
                table: "featured_deals",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_featured_deals_HotelId",
                table: "featured_deals",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_featured_deals_HotelRoomTypeId",
                table: "featured_deals",
                column: "HotelRoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_room_type_services_HotelRoomTypeId_HotelId",
                table: "hotel_room_type_services",
                columns: new[] { "HotelRoomTypeId", "HotelId" });

            migrationBuilder.CreateIndex(
                name: "IX_hotel_room_type_services_HotelRoomTypeId_HotelServiceId",
                table: "hotel_room_type_services",
                columns: new[] { "HotelRoomTypeId", "HotelServiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hotel_room_type_services_HotelServiceId_HotelId",
                table: "hotel_room_type_services",
                columns: new[] { "HotelServiceId", "HotelId" });

            migrationBuilder.CreateIndex(
                name: "IX_hotel_room_types_HotelId",
                table: "hotel_room_types",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_room_types_HotelId_RoomTypeId",
                table: "hotel_room_types",
                columns: new[] { "HotelId", "RoomTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hotel_room_types_Id_HotelId",
                table: "hotel_room_types",
                columns: new[] { "Id", "HotelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hotel_room_types_RoomTypeId",
                table: "hotel_room_types",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_services_HotelId_ServiceId",
                table: "hotel_services",
                columns: new[] { "HotelId", "ServiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hotel_services_Id_HotelId",
                table: "hotel_services",
                columns: new[] { "Id", "HotelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hotel_services_ServiceId",
                table: "hotel_services",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_visits_HotelId",
                table: "hotel_visits",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_visits_user_recent",
                table: "hotel_visits",
                columns: new[] { "UserId", "VisitedAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_hotel_visits_UserId_HotelId",
                table: "hotel_visits",
                columns: new[] { "UserId", "HotelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hotels_CityId_StarRating_MinPricePerNight",
                table: "hotels",
                columns: new[] { "CityId", "StarRating", "MinPricePerNight" },
                filter: "[DeletedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_hotels_Id_CityId",
                table: "hotels",
                columns: new[] { "Id", "CityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hotels_MinPricePerNight_Id",
                table: "hotels",
                columns: new[] { "MinPricePerNight", "Id" },
                filter: "[DeletedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_hotels_Name_CityId",
                table: "hotels",
                columns: new[] { "Name", "CityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_images_entity_sort",
                table: "images",
                columns: new[] { "EntityType", "EntityId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_room_types_Name",
                table: "room_types",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rooms_HotelId_RoomNumber",
                table: "rooms",
                columns: new[] { "HotelId", "RoomNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rooms_HotelRoomTypeId_HotelId",
                table: "rooms",
                columns: new[] { "HotelRoomTypeId", "HotelId" });

            migrationBuilder.CreateIndex(
                name: "IX_rooms_HotelRoomTypeId_Status",
                table: "rooms",
                columns: new[] { "HotelRoomTypeId", "Status" },
                filter: "[DeletedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_rooms_Id_HotelId",
                table: "rooms",
                columns: new[] { "Id", "HotelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_services_Name",
                table: "services",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "featured_deals");

            migrationBuilder.DropTable(
                name: "hotel_room_type_services");

            migrationBuilder.DropTable(
                name: "hotel_visits");

            migrationBuilder.DropTable(
                name: "images");

            migrationBuilder.DropTable(
                name: "rooms");

            migrationBuilder.DropTable(
                name: "hotel_services");

            migrationBuilder.DropTable(
                name: "hotel_room_types");

            migrationBuilder.DropTable(
                name: "services");

            migrationBuilder.DropTable(
                name: "hotels");

            migrationBuilder.DropTable(
                name: "room_types");

            migrationBuilder.DropTable(
                name: "cities");
        }
    }
}
