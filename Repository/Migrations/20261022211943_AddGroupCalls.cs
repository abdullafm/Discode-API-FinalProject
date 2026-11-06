using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupCalls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupCalls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    StarterId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Video = table.Column<bool>(type: "bit", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GroupMessageId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupCalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupCalls_AspNetUsers_StarterId",
                        column: x => x.StarterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupCalls_GroupMessages_GroupMessageId",
                        column: x => x.GroupMessageId,
                        principalTable: "GroupMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupCalls_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GroupCallParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupCallId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Screen = table.Column<bool>(type: "bit", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupCallParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupCallParticipants_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupCallParticipants_GroupCalls_GroupCallId",
                        column: x => x.GroupCallId,
                        principalTable: "GroupCalls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupCallParticipants_GroupCallId_UserId",
                table: "GroupCallParticipants",
                columns: new[] { "GroupCallId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupCallParticipants_UserId",
                table: "GroupCallParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupCalls_GroupId_Active",
                table: "GroupCalls",
                columns: new[] { "GroupId", "Active" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupCalls_GroupMessageId",
                table: "GroupCalls",
                column: "GroupMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupCalls_StarterId",
                table: "GroupCalls",
                column: "StarterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupCallParticipants");

            migrationBuilder.DropTable(
                name: "GroupCalls");
        }
    }
}
