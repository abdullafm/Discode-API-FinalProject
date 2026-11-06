using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCalls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Calls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CallerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CalleeId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Video = table.Column<bool>(type: "bit", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CallerIn = table.Column<bool>(type: "bit", nullable: false),
                    CalleeIn = table.Column<bool>(type: "bit", nullable: false),
                    CallerJoinable = table.Column<bool>(type: "bit", nullable: false),
                    CalleeJoinable = table.Column<bool>(type: "bit", nullable: false),
                    CallerScreen = table.Column<bool>(type: "bit", nullable: false),
                    CalleeScreen = table.Column<bool>(type: "bit", nullable: false),
                    MessageId = table.Column<int>(type: "int", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Calls_AspNetUsers_CalleeId",
                        column: x => x.CalleeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Calls_AspNetUsers_CallerId",
                        column: x => x.CallerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Calls_CalleeId",
                table: "Calls",
                column: "CalleeId");

            migrationBuilder.CreateIndex(
                name: "IX_Calls_CallerId_CalleeId",
                table: "Calls",
                columns: new[] { "CallerId", "CalleeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Calls_EndedAt",
                table: "Calls",
                column: "EndedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Calls");
        }
    }
}
