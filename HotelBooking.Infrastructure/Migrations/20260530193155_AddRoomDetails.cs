using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Amenities",
                table: "Rooms",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Rooms",
                type: "nvarchar(1200)",
                maxLength: 1200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAirConditioning",
                table: "Rooms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasBalcony",
                table: "Rooms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasPrivateBathroom",
                table: "Rooms",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasSaunaAccess",
                table: "Rooms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasWorkspace",
                table: "Rooms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludesBreakfast",
                table: "Rooms",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amenities",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "HasAirConditioning",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "HasBalcony",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "HasPrivateBathroom",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "HasSaunaAccess",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "HasWorkspace",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "IncludesBreakfast",
                table: "Rooms");
        }
    }
}
