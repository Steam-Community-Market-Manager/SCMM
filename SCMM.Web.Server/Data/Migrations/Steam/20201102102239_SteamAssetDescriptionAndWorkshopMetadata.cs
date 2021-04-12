using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class SteamAssetDescriptionAndWorkshopMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SteamAssetWorkshopFiles",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "SteamAssetWorkshopFiles",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "SteamAssetWorkshopFiles",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SteamAssetDescriptions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "SteamAssetDescriptions");
        }
    }
}
