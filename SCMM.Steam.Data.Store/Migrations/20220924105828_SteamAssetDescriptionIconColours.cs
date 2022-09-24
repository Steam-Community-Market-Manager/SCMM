using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamAssetDescriptionIconColours : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DominantColour",
                table: "SteamAssetDescriptions",
                newName: "IconAccentColour");

            migrationBuilder.AddColumn<string>(
                name: "IconDominantColours_Serialised",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IconDominantColours_Serialised",
                table: "SteamAssetDescriptions");

            migrationBuilder.RenameColumn(
                name: "IconAccentColour",
                table: "SteamAssetDescriptions",
                newName: "DominantColour");
        }
    }
}
