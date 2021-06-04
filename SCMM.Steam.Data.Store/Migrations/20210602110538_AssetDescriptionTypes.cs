using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AssetDescriptionTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "SteamAssetDescriptions",
                newName: "AssetType");

            migrationBuilder.AddColumn<string>(
                name: "ItemType",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "SteamAssetDescriptions");

            migrationBuilder.RenameColumn(
                name: "AssetType",
                table: "SteamAssetDescriptions",
                newName: "Type");
        }
    }
}
