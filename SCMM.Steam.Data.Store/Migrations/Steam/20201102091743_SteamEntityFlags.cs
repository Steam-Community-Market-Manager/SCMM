using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations.Steam
{
    public partial class SteamEntityFlags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "Flags",
                table: "SteamStoreItems",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "Flags",
                table: "SteamProfiles",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "Flags",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "Flags",
                table: "SteamInventoryItems",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "Flags",
                table: "SteamAssetWorkshopFiles",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "Flags",
                table: "SteamAssetDescriptions",
                nullable: false,
                defaultValue: (byte)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Flags",
                table: "SteamStoreItems");

            migrationBuilder.DropColumn(
                name: "Flags",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "Flags",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Flags",
                table: "SteamInventoryItems");

            migrationBuilder.DropColumn(
                name: "Flags",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropColumn(
                name: "Flags",
                table: "SteamAssetDescriptions");
        }
    }
}
