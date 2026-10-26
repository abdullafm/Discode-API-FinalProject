using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddServerRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServerRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServerId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "gray"),
                    Permissions = table.Column<long>(type: "bigint", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    IsEveryone = table.Column<bool>(type: "bit", nullable: false),
                    DisplaySeparately = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerRoles_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServerMemberRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServerMemberId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerMemberRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerMemberRoles_ServerMembers_ServerMemberId",
                        column: x => x.ServerMemberId,
                        principalTable: "ServerMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServerMemberRoles_ServerRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "ServerRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServerMemberRoles_RoleId",
                table: "ServerMemberRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerMemberRoles_ServerMemberId_RoleId",
                table: "ServerMemberRoles",
                columns: new[] { "ServerMemberId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServerRoles_ServerId_Name",
                table: "ServerRoles",
                columns: new[] { "ServerId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServerMemberRoles");

            migrationBuilder.DropTable(
                name: "ServerRoles");
        }
    }
}
