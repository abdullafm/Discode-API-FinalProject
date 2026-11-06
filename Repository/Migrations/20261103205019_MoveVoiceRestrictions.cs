using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class MoveVoiceRestrictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServerVoiceRestrictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServerId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Muted = table.Column<bool>(type: "bit", nullable: false),
                    Deafened = table.Column<bool>(type: "bit", nullable: false),
                    ByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerVoiceRestrictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerVoiceRestrictions_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServerVoiceRestrictions_ServerId_UserId",
                table: "ServerVoiceRestrictions",
                columns: new[] { "ServerId", "UserId" },
                unique: true);

            // mutes that already exist keep working: they move from the membership to the new table
            migrationBuilder.Sql(@"INSERT INTO ServerVoiceRestrictions (ServerId, UserId, Muted, Deafened, ByUserId, CreatedAt)
                SELECT m.ServerId, m.UserId, m.VoiceMuted, m.VoiceDeafened, s.OwnerId, SYSUTCDATETIME()
                FROM ServerMembers m JOIN Servers s ON s.Id = m.ServerId
                WHERE m.VoiceMuted = 1 OR m.VoiceDeafened = 1");

            migrationBuilder.DropColumn(
                name: "VoiceDeafened",
                table: "ServerMembers");

            migrationBuilder.DropColumn(
                name: "VoiceMuted",
                table: "ServerMembers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServerVoiceRestrictions");

            migrationBuilder.AddColumn<bool>(
                name: "VoiceDeafened",
                table: "ServerMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VoiceMuted",
                table: "ServerMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
