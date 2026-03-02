using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingPaymentCancellationConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingService_bookings_BookingId",
                table: "BookingService");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingService_hotel_services_HotelServiceId",
                table: "BookingService");

            migrationBuilder.DropForeignKey(
                name: "FK_Cancellation_bookings_BookingId",
                table: "Cancellation");

            migrationBuilder.DropForeignKey(
                name: "FK_Payment_bookings_BookingId",
                table: "Payment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Payment",
                table: "Payment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cancellation",
                table: "Cancellation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BookingService",
                table: "BookingService");

            migrationBuilder.DropIndex(
                name: "IX_BookingService_HotelServiceId",
                table: "BookingService");

            migrationBuilder.RenameTable(
                name: "Payment",
                newName: "payments");

            migrationBuilder.RenameTable(
                name: "Cancellation",
                newName: "cancellations");

            migrationBuilder.RenameTable(
                name: "BookingService",
                newName: "booking_services");

            migrationBuilder.RenameIndex(
                name: "IX_Payment_BookingId",
                table: "payments",
                newName: "IX_payments_BookingId");

            migrationBuilder.RenameIndex(
                name: "IX_Cancellation_BookingId",
                table: "cancellations",
                newName: "IX_cancellations_BookingId");

            migrationBuilder.RenameIndex(
                name: "IX_BookingService_BookingId",
                table: "booking_services",
                newName: "IX_booking_services_BookingId");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionRef",
                table: "payments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "payments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderSessionId",
                table: "payments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Method",
                table: "payments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<Guid>(
                name: "BookingId1",
                table: "payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RefundStatus",
                table: "cancellations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "RefundPercentage",
                table: "cancellations",
                type: "decimal(5,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "cancellations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BookingId1",
                table: "cancellations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ServiceName",
                table: "booking_services",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payments",
                table: "payments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_cancellations",
                table: "cancellations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_booking_services",
                table: "booking_services",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_BookingId1",
                table: "payments",
                column: "BookingId1");

            migrationBuilder.CreateIndex(
                name: "IX_payments_ProviderSessionId",
                table: "payments",
                column: "ProviderSessionId",
                filter: "[ProviderSessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payments_Status",
                table: "payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_payments_TransactionRef",
                table: "payments",
                column: "TransactionRef",
                unique: true,
                filter: "[TransactionRef] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_cancellations_BookingId1",
                table: "cancellations",
                column: "BookingId1",
                unique: true,
                filter: "[BookingId1] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_booking_services_BookingId_HotelServiceId",
                table: "booking_services",
                columns: new[] { "BookingId", "HotelServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_services_HotelId_HotelServiceId",
                table: "booking_services",
                columns: new[] { "HotelId", "HotelServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_services_HotelServiceId_HotelId",
                table: "booking_services",
                columns: new[] { "HotelServiceId", "HotelId" });

            migrationBuilder.AddForeignKey(
                name: "FK_booking_services_bookings_BookingId",
                table: "booking_services",
                column: "BookingId",
                principalTable: "bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_booking_services_hotel_services_HotelServiceId_HotelId",
                table: "booking_services",
                columns: new[] { "HotelServiceId", "HotelId" },
                principalTable: "hotel_services",
                principalColumns: new[] { "Id", "HotelId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_cancellations_bookings_BookingId",
                table: "cancellations",
                column: "BookingId",
                principalTable: "bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_cancellations_bookings_BookingId1",
                table: "cancellations",
                column: "BookingId1",
                principalTable: "bookings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_payments_bookings_BookingId",
                table: "payments",
                column: "BookingId",
                principalTable: "bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_bookings_BookingId1",
                table: "payments",
                column: "BookingId1",
                principalTable: "bookings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_booking_services_bookings_BookingId",
                table: "booking_services");

            migrationBuilder.DropForeignKey(
                name: "FK_booking_services_hotel_services_HotelServiceId_HotelId",
                table: "booking_services");

            migrationBuilder.DropForeignKey(
                name: "FK_cancellations_bookings_BookingId",
                table: "cancellations");

            migrationBuilder.DropForeignKey(
                name: "FK_cancellations_bookings_BookingId1",
                table: "cancellations");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_bookings_BookingId",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_bookings_BookingId1",
                table: "payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payments",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_BookingId1",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_ProviderSessionId",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_Status",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_TransactionRef",
                table: "payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_cancellations",
                table: "cancellations");

            migrationBuilder.DropIndex(
                name: "IX_cancellations_BookingId1",
                table: "cancellations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_booking_services",
                table: "booking_services");

            migrationBuilder.DropIndex(
                name: "IX_booking_services_BookingId_HotelServiceId",
                table: "booking_services");

            migrationBuilder.DropIndex(
                name: "IX_booking_services_HotelId_HotelServiceId",
                table: "booking_services");

            migrationBuilder.DropIndex(
                name: "IX_booking_services_HotelServiceId_HotelId",
                table: "booking_services");

            migrationBuilder.DropColumn(
                name: "BookingId1",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "BookingId1",
                table: "cancellations");

            migrationBuilder.RenameTable(
                name: "payments",
                newName: "Payment");

            migrationBuilder.RenameTable(
                name: "cancellations",
                newName: "Cancellation");

            migrationBuilder.RenameTable(
                name: "booking_services",
                newName: "BookingService");

            migrationBuilder.RenameIndex(
                name: "IX_payments_BookingId",
                table: "Payment",
                newName: "IX_Payment_BookingId");

            migrationBuilder.RenameIndex(
                name: "IX_cancellations_BookingId",
                table: "Cancellation",
                newName: "IX_Cancellation_BookingId");

            migrationBuilder.RenameIndex(
                name: "IX_booking_services_BookingId",
                table: "BookingService",
                newName: "IX_BookingService_BookingId");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionRef",
                table: "Payment",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Payment",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderSessionId",
                table: "Payment",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Method",
                table: "Payment",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<int>(
                name: "RefundStatus",
                table: "Cancellation",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<decimal>(
                name: "RefundPercentage",
                table: "Cancellation",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Cancellation",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ServiceName",
                table: "BookingService",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Payment",
                table: "Payment",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cancellation",
                table: "Cancellation",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BookingService",
                table: "BookingService",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BookingService_HotelServiceId",
                table: "BookingService",
                column: "HotelServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingService_bookings_BookingId",
                table: "BookingService",
                column: "BookingId",
                principalTable: "bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingService_hotel_services_HotelServiceId",
                table: "BookingService",
                column: "HotelServiceId",
                principalTable: "hotel_services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cancellation_bookings_BookingId",
                table: "Cancellation",
                column: "BookingId",
                principalTable: "bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payment_bookings_BookingId",
                table: "Payment",
                column: "BookingId",
                principalTable: "bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
