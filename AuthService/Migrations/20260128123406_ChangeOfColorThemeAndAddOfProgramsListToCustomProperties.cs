using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Migrations
{
    /// <inheritdoc />
    public partial class ChangeOfColorThemeAndAddOfProgramsListToCustomProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ColorThemeCode",
                table: "AspNetUsersAppSettings",
                newName: "PreferredColorThemeCode");

            migrationBuilder.AddColumn<List<string>>(
                name: "Programs",
                table: "AspNetUsersCustomProperties",
                type: "text[]",
                nullable: false,
                defaultValue: new List<string>());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Programs",
                table: "AspNetUsersCustomProperties");

            migrationBuilder.RenameColumn(
                name: "PreferredColorThemeCode",
                table: "AspNetUsersAppSettings",
                newName: "ColorThemeCode");
        }
    }
}
