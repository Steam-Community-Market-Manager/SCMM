using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamAssetDescriptionWorkshopFileAnalyticProperties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CutoutRatio",
                table: "SteamAssetDescriptions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DominantColour",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GlowRatio",
                table: "SteamAssetDescriptions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasCutout",
                table: "SteamAssetDescriptions",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasGlow",
                table: "SteamAssetDescriptions",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasGlowSights",
                table: "SteamAssetDescriptions",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CutoutRatio",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "DominantColour",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "GlowRatio",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "HasCutout",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "HasGlow",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "HasGlowSights",
                table: "SteamAssetDescriptions");
        }
    }
}
