using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamAssetDescriptionItemDefinitionModelUpdates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AssetType",
                table: "SteamAssetDescriptions",
                newName: "ItemDefinitionType");

            migrationBuilder.Sql("UPDATE [SteamAssetDescriptions] SET [ItemDefinitionType] = 0");

            migrationBuilder.AddColumn<string>(
                name: "Bundle_Serialised",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bundle_Serialised",
                table: "SteamAssetDescriptions");

            migrationBuilder.Sql("UPDATE [SteamAssetDescriptions] SET [AssetType] = 0");

            migrationBuilder.RenameColumn(
                name: "ItemDefinitionType",
                table: "SteamAssetDescriptions",
                newName: "AssetType");
        }
    }
}
