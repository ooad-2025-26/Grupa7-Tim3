using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkirajBa.Migrations
{
    /// <inheritdoc />
    public partial class ParkingRequestAndIsAvailable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isApproved",
                table: "ParkingObject",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isApproved",
                table: "ParkingObject");
        }
    }
}
