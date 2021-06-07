using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamAssetDescriptionCraftingComponents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CraftingRequirements_Serialised",
                table: "SteamAssetDescriptions",
                newName: "CraftingComponents_Serialised");

            migrationBuilder.RenameColumn(
                name: "BreaksDownInto_Serialised",
                table: "SteamAssetDescriptions",
                newName: "BreaksIntoComponents_Serialised");

            migrationBuilder.AddColumn<bool>(
                name: "IsCraftingComponent",
                table: "SteamAssetDescriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCraftingComponent",
                table: "SteamAssetDescriptions");

            migrationBuilder.RenameColumn(
                name: "CraftingComponents_Serialised",
                table: "SteamAssetDescriptions",
                newName: "CraftingRequirements_Serialised");

            migrationBuilder.RenameColumn(
                name: "BreaksIntoComponents_Serialised",
                table: "SteamAssetDescriptions",
                newName: "BreaksDownInto_Serialised");
        }
    }
}
