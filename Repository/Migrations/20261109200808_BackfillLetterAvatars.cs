using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class BackfillLetterAvatars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE AspNetUsers SET AvatarUrl = '/avatar/letter/' + CASE WHEN LEFT(UserName, 1) LIKE '[A-Za-z0-9]' THEN UPPER(LEFT(UserName, 1)) ELSE '_' END WHERE AvatarUrl = ''");


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
