using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SelenicSparkApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class IdentityUserExpanderSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdentityUserExpander",
                columns: table => new
                {
                    UID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UsernameChangeTokens = table.Column<int>(type: "int", nullable: false),
                    UserWarningsCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityUserExpander", x => x.UID);
                    table.ForeignKey(
                        name: "FK_IdentityUserExpander_AspNetUsers_UID",
                        column: x => x.UID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IdentityUserExpander_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityUserExpander_UserId",
                table: "IdentityUserExpander",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentityUserExpander");
        }
    }
}
