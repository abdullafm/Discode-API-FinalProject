using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIgnores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserIgnores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IgnorerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IgnoredId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserIgnores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserIgnores_AspNetUsers_IgnoredId",
                        column: x => x.IgnoredId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserIgnores_AspNetUsers_IgnorerId",
                        column: x => x.IgnorerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserIgnores_IgnoredId",
                table: "UserIgnores",
                column: "IgnoredId");

            migrationBuilder.CreateIndex(
                name: "IX_UserIgnores_IgnorerId_IgnoredId",
                table: "UserIgnores",
                columns: new[] { "IgnorerId", "IgnoredId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserIgnores");
        }
    }
}
