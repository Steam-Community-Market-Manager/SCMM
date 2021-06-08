using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamAssetDescriptionRename : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimeChecked",
                table: "SteamAssetDescriptions",
                newName: "TimeRefreshed");

            migrationBuilder.RenameColumn(
                name: "AssetId",
                table: "SteamAssetDescriptions",
                newName: "ClassId");

            migrationBuilder.RenameIndex(
                name: "IX_SteamAssetDescriptions_AssetId",
                table: "SteamAssetDescriptions",
                newName: "IX_SteamAssetDescriptions_ClassId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimeRefreshed",
                table: "SteamAssetDescriptions",
                newName: "TimeChecked");

            migrationBuilder.RenameColumn(
                name: "ClassId",
                table: "SteamAssetDescriptions",
                newName: "AssetId");

            migrationBuilder.RenameIndex(
                name: "IX_SteamAssetDescriptions_ClassId",
                table: "SteamAssetDescriptions",
                newName: "IX_SteamAssetDescriptions_AssetId");
        }
    }
}
