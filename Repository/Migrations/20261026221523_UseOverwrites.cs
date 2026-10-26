using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class UseOverwrites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SyncWithCategory",
                table: "ServerChannels",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "ServerOverwrites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServerId = table.Column<int>(type: "int", nullable: false),
                    ChannelId = table.Column<int>(type: "int", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Allow = table.Column<long>(type: "bigint", nullable: false),
                    Deny = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerOverwrites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerOverwrites_ServerChannels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "ServerChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServerOverwrites_CategoryId",
                table: "ServerOverwrites",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerOverwrites_ChannelId",
                table: "ServerOverwrites",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerOverwrites_RoleId",
                table: "ServerOverwrites",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerOverwrites_ServerId",
                table: "ServerOverwrites",
                column: "ServerId");

            migrationBuilder.Sql(@"
INSERT INTO ServerRoles (ServerId, Name, Color, Permissions, Position, IsEveryone, DisplaySeparately, CreatedAt)
SELECT s.Id, '@everyone', 'gray', 6401, 0, 1, 0, GETUTCDATE() FROM Servers s
WHERE NOT EXISTS (SELECT 1 FROM ServerRoles r WHERE r.ServerId = s.Id AND r.IsEveryone = 1);

UPDATE ServerChannels SET SyncWithCategory = 0 WHERE IsPrivate = 1 OR IsReadOnly = 1;

INSERT INTO ServerOverwrites (ServerId, CategoryId, RoleId, [Allow], [Deny], CreatedAt)
SELECT c.ServerId, c.Id, r.Id, 0, 1, GETUTCDATE()
FROM ServerCategories c JOIN ServerRoles r ON r.ServerId = c.ServerId AND r.IsEveryone = 1
WHERE c.IsPrivate = 1;

INSERT INTO ServerOverwrites (ServerId, ChannelId, RoleId, [Allow], [Deny], CreatedAt)
SELECT ch.ServerId, ch.Id, r.Id, 0,
    (CASE WHEN ch.IsPrivate = 1 THEN 1 ELSE 0 END) + (CASE WHEN ch.IsReadOnly = 1 THEN 256 ELSE 0 END),
    GETUTCDATE()
FROM ServerChannels ch JOIN ServerRoles r ON r.ServerId = ch.ServerId AND r.IsEveryone = 1
WHERE ch.IsPrivate = 1 OR ch.IsReadOnly = 1;

INSERT INTO ServerOverwrites (ServerId, ChannelId, RoleId, UserId, [Allow], [Deny], CreatedAt)
SELECT ch.ServerId, a.ChannelId, a.RoleId, a.UserId, 1, 0, a.CreatedAt
FROM ServerChannelAccesses a JOIN ServerChannels ch ON ch.Id = a.ChannelId;

INSERT INTO ServerOverwrites (ServerId, ChannelId, RoleId, [Allow], [Deny], CreatedAt)
SELECT ch.ServerId, ch.Id, r.Id, 256, 0, GETUTCDATE()
FROM ServerChannels ch JOIN ServerRoles r ON r.ServerId = ch.ServerId AND r.IsEveryone = 0 AND (r.Permissions & 512) <> 0
WHERE ch.IsReadOnly = 1;
");

            migrationBuilder.DropTable(
                name: "ServerChannelAccesses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServerOverwrites");

            migrationBuilder.DropColumn(
                name: "SyncWithCategory",
                table: "ServerChannels");

            migrationBuilder.CreateTable(
                name: "ServerChannelAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChannelId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
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
    }
}
