using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkirajBa.Migrations
{
    /// <inheritdoc />
    public partial class TicketParkingTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalCharge",
                table: "Tickets",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "AdditionalChargePaid",
                table: "Tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "EnteredAt",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnteredParking",
                table: "Tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExitedAt",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExitedParking",
                table: "Tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "QrCodeActive",
                table: "Tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalCharge",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "AdditionalChargePaid",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EnteredAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EnteredParking",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ExitedAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ExitedParking",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "QrCodeActive",
                table: "Tickets");
        }
    }
}
