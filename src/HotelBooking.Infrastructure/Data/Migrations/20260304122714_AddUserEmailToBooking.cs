using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEmailToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                table: "bookings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
            migrationBuilder.Sql("""
                UPDATE b
                SET b.UserEmail = u.Email
                FROM bookings b
                INNER JOIN AspNetUsers u ON u.Id = b.UserId
                WHERE b.UserEmail IS NULL
            """);

            migrationBuilder.Sql("""
                UPDATE bookings
                SET UserEmail = 'unknown@hotelbooking.local'
                WHERE UserEmail IS NULL OR LTRIM(RTRIM(UserEmail)) = ''
            """);

            migrationBuilder.AlterColumn<string>(
                name: "UserEmail",
                table: "bookings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);




        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserEmail",
                table: "bookings");
        }
    }
}
