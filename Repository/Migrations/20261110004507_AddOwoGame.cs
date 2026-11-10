using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddOwoGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OwoEnabled",
                table: "Servers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "OwoAnimals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AnimalKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwoAnimals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OwoAnimals_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OwoProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Cowoncy = table.Column<long>(type: "bigint", nullable: false),
                    Luck = table.Column<int>(type: "int", nullable: false),
                    DailyStreak = table.Column<int>(type: "int", nullable: false),
                    Hunts = table.Column<int>(type: "int", nullable: false),
                    BattlesWon = table.Column<int>(type: "int", nullable: false),
                    BattlesLost = table.Column<int>(type: "int", nullable: false),
                    LastDailyAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastHuntAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastBattleAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastPrayAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastOwoAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastGambleAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwoProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OwoProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OwoAnimals_UserId_AnimalKey",
                table: "OwoAnimals",
                columns: new[] { "UserId", "AnimalKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwoProfiles_Cowoncy",
                table: "OwoProfiles",
                column: "Cowoncy");

            migrationBuilder.CreateIndex(
                name: "IX_OwoProfiles_UserId",
                table: "OwoProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwoAnimals");

            migrationBuilder.DropTable(
                name: "OwoProfiles");

            migrationBuilder.DropColumn(
                name: "OwoEnabled",
                table: "Servers");
        }
    }
}
