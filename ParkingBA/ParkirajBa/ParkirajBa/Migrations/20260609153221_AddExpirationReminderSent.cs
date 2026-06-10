using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkirajBa.Migrations
{
    /// <inheritdoc />
    public partial class AddExpirationReminderSent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ExpirationReminderSent",
                table: "Tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpirationReminderSent",
                table: "Tickets");
        }
    }
}
