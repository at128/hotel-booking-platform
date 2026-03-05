using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_Status",
                table: "payments");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "payments",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_payments_Status_CreatedAtUtc",
                table: "payments",
                columns: new[] { "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_Status_CreatedAtUtc",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "payments");

            migrationBuilder.CreateIndex(
                name: "IX_payments_Status",
                table: "payments",
                column: "Status");
        }
    }
}
