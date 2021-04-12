using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class SteamAssetDescriptionItemLinkback : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SteamStoreItems_DescriptionId",
                table: "SteamStoreItems");

            migrationBuilder.DropIndex(
                name: "IX_SteamMarketItems_DescriptionId",
                table: "SteamMarketItems");

            migrationBuilder.CreateIndex(
                name: "IX_SteamStoreItems_DescriptionId",
                table: "SteamStoreItems",
                column: "DescriptionId",
                unique: true,
                filter: "[DescriptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItems_DescriptionId",
                table: "SteamMarketItems",
                column: "DescriptionId",
                unique: true,
                filter: "[DescriptionId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SteamStoreItems_DescriptionId",
                table: "SteamStoreItems");

            migrationBuilder.DropIndex(
                name: "IX_SteamMarketItems_DescriptionId",
                table: "SteamMarketItems");

            migrationBuilder.CreateIndex(
                name: "IX_SteamStoreItems_DescriptionId",
                table: "SteamStoreItems",
                column: "DescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItems_DescriptionId",
                table: "SteamMarketItems",
                column: "DescriptionId");
        }
    }
}
