using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkirajBa.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationCodeAndPaidAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReservationCode",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ReservationCode",
                table: "Tickets");
        }
    }
}
