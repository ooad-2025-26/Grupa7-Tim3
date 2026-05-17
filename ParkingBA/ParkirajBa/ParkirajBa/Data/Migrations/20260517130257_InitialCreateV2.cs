using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkirajBa.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_ApplicationUser_ApplicationUserId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "ApplicationUser");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Owner");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Owner");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Owner");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Owner");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Administrator");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Administrator");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Administrator");

            migrationBuilder.DropColumn(
                name: "accessLevel",
                table: "Administrator");

            migrationBuilder.RenameColumn(
                name: "companyName",
                table: "Owner",
                newName: "CompanyName");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "Owner",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "Administrator",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "Administrator",
                newName: "Department");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Owner",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Administrator",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Owner_UserId",
                table: "Owner",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Administrator_UserId",
                table: "Administrator",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Administrator_AspNetUsers_UserId",
                table: "Administrator",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Owner_AspNetUsers_UserId",
                table: "Owner",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_AspNetUsers_ApplicationUserId",
                table: "Tickets",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Administrator_AspNetUsers_UserId",
                table: "Administrator");

            migrationBuilder.DropForeignKey(
                name: "FK_Owner_AspNetUsers_UserId",
                table: "Owner");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_AspNetUsers_ApplicationUserId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Owner_UserId",
                table: "Owner");

            migrationBuilder.DropIndex(
                name: "IX_Administrator_UserId",
                table: "Administrator");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Owner");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Administrator");

            migrationBuilder.RenameColumn(
                name: "CompanyName",
                table: "Owner",
                newName: "companyName");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Owner",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Administrator",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "Department",
                table: "Administrator",
                newName: "PasswordHash");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Owner",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Owner",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Owner",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Owner",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Administrator",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Administrator",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Administrator",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "accessLevel",
                table: "Administrator",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ApplicationUser",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUser", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_ApplicationUser_ApplicationUserId",
                table: "Tickets",
                column: "ApplicationUserId",
                principalTable: "ApplicationUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
