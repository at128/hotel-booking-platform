using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutHoldActiveIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_checkout_holds_HotelId_IsReleased_ExpiresAtUtc",
                table: "checkout_holds",
                columns: new[] { "HotelId", "IsReleased", "ExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_checkout_holds_HotelId_IsReleased_ExpiresAtUtc",
                table: "checkout_holds");
        }
    }
}
