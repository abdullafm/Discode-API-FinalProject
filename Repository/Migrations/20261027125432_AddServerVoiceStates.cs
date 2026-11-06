using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddServerVoiceStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServerVoiceStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServerId = table.Column<int>(type: "int", nullable: false),
                    ChannelId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsMuted = table.Column<bool>(type: "bit", nullable: false),
                    IsDeafened = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerVoiceStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerVoiceStates_ServerChannels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "ServerChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServerVoiceStates_ChannelId",
                table: "ServerVoiceStates",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerVoiceStates_ServerId",
                table: "ServerVoiceStates",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerVoiceStates_UserId",
                table: "ServerVoiceStates",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServerVoiceStates");
        }
    }
}
