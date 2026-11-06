using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupMessageFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Polls_MessageId",
                table: "Polls");

            migrationBuilder.AlterColumn<int>(
                name: "MessageId",
                table: "Polls",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "GroupMessageId",
                table: "Polls",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GroupMessageAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupMessageId = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMessageAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMessageAttachments_GroupMessages_GroupMessageId",
                        column: x => x.GroupMessageId,
                        principalTable: "GroupMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupMessageReactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupMessageId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Emoji = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMessageReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMessageReactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupMessageReactions_GroupMessages_GroupMessageId",
                        column: x => x.GroupMessageId,
                        principalTable: "GroupMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Polls_GroupMessageId",
                table: "Polls",
                column: "GroupMessageId",
                unique: true,
                filter: "[GroupMessageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Polls_MessageId",
                table: "Polls",
                column: "MessageId",
                unique: true,
                filter: "[MessageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMessageAttachments_GroupMessageId",
                table: "GroupMessageAttachments",
                column: "GroupMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMessageReactions_GroupMessageId_UserId_Emoji",
                table: "GroupMessageReactions",
                columns: new[] { "GroupMessageId", "UserId", "Emoji" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMessageReactions_UserId",
                table: "GroupMessageReactions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Polls_GroupMessages_GroupMessageId",
                table: "Polls",
                column: "GroupMessageId",
                principalTable: "GroupMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Polls_GroupMessages_GroupMessageId",
                table: "Polls");

            migrationBuilder.DropTable(
                name: "GroupMessageAttachments");

            migrationBuilder.DropTable(
                name: "GroupMessageReactions");

            migrationBuilder.DropIndex(
                name: "IX_Polls_GroupMessageId",
                table: "Polls");

            migrationBuilder.DropIndex(
                name: "IX_Polls_MessageId",
                table: "Polls");

            migrationBuilder.DropColumn(
                name: "GroupMessageId",
                table: "Polls");

            migrationBuilder.AlterColumn<int>(
                name: "MessageId",
                table: "Polls",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Polls_MessageId",
                table: "Polls",
                column: "MessageId",
                unique: true);
        }
    }
}
