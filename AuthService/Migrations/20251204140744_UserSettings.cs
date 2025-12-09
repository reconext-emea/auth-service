using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Migrations
{
    /// <inheritdoc />
    public partial class UserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetUsersAppSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PreferredLanguageCode = table.Column<string>(type: "text", nullable: false, defaultValue: "en"),
                    ColorThemeCode = table.Column<string>(type: "text", nullable: false, defaultValue: "light")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsersAppSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsersAppSettings_AspNetUsers_Id",
                        column: x => x.Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetUsersAppSettings");
        }
    }
}
