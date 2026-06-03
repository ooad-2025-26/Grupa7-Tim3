using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkirajBa.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPaidAndStartsAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "Tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "totalSpots",
                table: "ParkingObject",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<double>(
                name: "longitude",
                table: "ParkingObject",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "latitude",
                table: "ParkingObject",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "ParkingObject",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "availableSpots",
                table: "ParkingObject",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingObject_OwnerId",
                table: "ParkingObject",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingObject_AspNetUsers_OwnerId",
                table: "ParkingObject",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingObject_AspNetUsers_OwnerId",
                table: "ParkingObject");

            migrationBuilder.DropIndex(
                name: "IX_ParkingObject_OwnerId",
                table: "ParkingObject");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "ParkingObject");

            migrationBuilder.DropColumn(
                name: "availableSpots",
                table: "ParkingObject");

            migrationBuilder.AlterColumn<int>(
                name: "totalSpots",
                table: "ParkingObject",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "longitude",
                table: "ParkingObject",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "latitude",
                table: "ParkingObject",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");
        }
    }
}
