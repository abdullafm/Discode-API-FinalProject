using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddServerMemberPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HideMutedChannels",
                table: "ServerMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MutedIndefinitely",
                table: "ServerMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "MutedUntil",
                table: "ServerMembers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NotificationLevel",
                table: "ServerMembers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "ServerMembers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ShowAllChannels",
                table: "ServerMembers",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HideMutedChannels",
                table: "ServerMembers");

            migrationBuilder.DropColumn(
                name: "MutedIndefinitely",
                table: "ServerMembers");

            migrationBuilder.DropColumn(
                name: "MutedUntil",
                table: "ServerMembers");

            migrationBuilder.DropColumn(
                name: "NotificationLevel",
                table: "ServerMembers");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "ServerMembers");

            migrationBuilder.DropColumn(
                name: "ShowAllChannels",
                table: "ServerMembers");
        }
    }
}
