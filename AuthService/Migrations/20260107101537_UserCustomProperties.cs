using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Migrations
{
    /// <inheritdoc />
    public partial class UserCustomProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetUsersCustomProperties",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Confidentiality = table.Column<string>(type: "text", nullable: false, defaultValue: "Class 1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsersCustomProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsersCustomProperties_AspNetUsers_Id",
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
                name: "AspNetUsersCustomProperties");
        }
    }
}
