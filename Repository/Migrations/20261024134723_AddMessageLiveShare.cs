using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageLiveShare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LiveHostName",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LiveImageUrl",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LiveStreamId",
                table: "Messages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LiveTitle",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LiveHostName",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "LiveImageUrl",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "LiveStreamId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "LiveTitle",
                table: "Messages");
        }
    }
}
