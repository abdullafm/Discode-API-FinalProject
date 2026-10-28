using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddServerPolls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ServerMessageId",
                table: "Polls",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Polls_ServerMessageId",
                table: "Polls",
                column: "ServerMessageId",
                unique: true,
                filter: "[ServerMessageId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Polls_ServerMessages_ServerMessageId",
                table: "Polls",
                column: "ServerMessageId",
                principalTable: "ServerMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Polls_ServerMessages_ServerMessageId",
                table: "Polls");

            migrationBuilder.DropIndex(
                name: "IX_Polls_ServerMessageId",
                table: "Polls");

            migrationBuilder.DropColumn(
                name: "ServerMessageId",
                table: "Polls");
        }
    }
}
