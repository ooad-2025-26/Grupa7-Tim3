using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkirajBa.Data.Migrations
{
    /// <inheritdoc />
    public partial class ParkirajBaMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParkingObject",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    latitude = table.Column<double>(type: "float", nullable: true),
                    longitude = table.Column<double>(type: "float", nullable: true),
                    totalSpots = table.Column<int>(type: "int", nullable: false),
                    hasCameras = table.Column<bool>(type: "bit", nullable: true),
                    isDisabledAccessible = table.Column<bool>(type: "bit", nullable: true),
                    hasEVCharger = table.Column<bool>(type: "bit", nullable: true),
                    maxHeight = table.Column<double>(type: "float", nullable: true),
                    isUnderground = table.Column<bool>(type: "bit", nullable: true),
                    opensAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    closesAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingObject", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Pricing",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    pricingType = table.Column<int>(type: "int", nullable: false),
                    price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    validFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    validTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ParkingObjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pricing", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Pricing_ParkingObject_ParkingObjectID",
                        column: x => x.ParkingObjectID,
                        principalTable: "ParkingObject",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pricing_ParkingObjectID",
                table: "Pricing",
                column: "ParkingObjectID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pricing");

            migrationBuilder.DropTable(
                name: "ParkingObject");
        }
    }
}
