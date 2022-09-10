using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamProfileInventoryValueUniqueConstraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SteamProfileInventoryValues_ProfileId",
                table: "SteamProfileInventoryValues");

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfileInventoryValues_ProfileId_AppId",
                table: "SteamProfileInventoryValues",
                columns: new[] { "ProfileId", "AppId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SteamProfileInventoryValues_ProfileId_AppId",
                table: "SteamProfileInventoryValues");

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfileInventoryValues_ProfileId",
                table: "SteamProfileInventoryValues",
                column: "ProfileId");
        }
    }
}
