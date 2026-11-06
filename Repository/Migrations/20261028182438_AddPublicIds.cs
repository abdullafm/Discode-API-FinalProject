using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "Servers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "ServerMessages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "Messages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "GroupMessages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            foreach (var table in new[] { "Servers", "ServerMessages", "Messages", "GroupMessages", "AspNetUsers" })
            {
                migrationBuilder.Sql(
                    $"UPDATE [{table}] SET [PublicId] = CAST(100000000000000000 + ABS(CAST(CRYPT_GEN_RANDOM(8) AS bigint) % 900000000000000000) AS nvarchar(20))");
            }

            migrationBuilder.CreateIndex(
                name: "IX_Servers_PublicId",
                table: "Servers",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServerMessages_PublicId",
                table: "ServerMessages",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_PublicId",
                table: "Messages",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMessages_PublicId",
                table: "GroupMessages",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PublicId",
                table: "AspNetUsers",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Servers_PublicId",
                table: "Servers");

            migrationBuilder.DropIndex(
                name: "IX_ServerMessages_PublicId",
                table: "ServerMessages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_PublicId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_GroupMessages_PublicId",
                table: "GroupMessages");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PublicId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "ServerMessages");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "GroupMessages");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "AspNetUsers");
        }
    }
}
