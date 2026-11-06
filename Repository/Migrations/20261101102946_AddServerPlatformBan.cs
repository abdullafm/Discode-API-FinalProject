using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddServerPlatformBan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BanReason",
                table: "Servers",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "BannedAt",
                table: "Servers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannedById",
                table: "Servers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBanned",
                table: "Servers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Servers_IsBanned",
                table: "Servers",
                column: "IsBanned");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Servers_IsBanned",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "BanReason",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "BannedAt",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "BannedById",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "IsBanned",
                table: "Servers");
        }
    }
}
