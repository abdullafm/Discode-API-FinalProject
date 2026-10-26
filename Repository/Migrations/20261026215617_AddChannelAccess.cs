using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServerChannelAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChannelId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerChannelAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerChannelAccesses_ServerChannels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "ServerChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServerChannelAccesses_ChannelId",
                table: "ServerChannelAccesses",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerChannelAccesses_RoleId",
                table: "ServerChannelAccesses",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerChannelAccesses_UserId",
                table: "ServerChannelAccesses",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServerChannelAccesses");
        }
    }
}
