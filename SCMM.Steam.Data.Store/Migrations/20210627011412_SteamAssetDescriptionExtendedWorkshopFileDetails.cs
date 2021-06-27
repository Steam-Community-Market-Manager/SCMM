using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamAssetDescriptionExtendedWorkshopFileDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CurrentFavourited",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionWorkshop",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LifetimeFavourited",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameWorkshop",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviewContentId",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Views",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentFavourited",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "DescriptionWorkshop",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "LifetimeFavourited",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "NameWorkshop",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "PreviewContentId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "Views",
                table: "SteamAssetDescriptions");
        }
    }
}
