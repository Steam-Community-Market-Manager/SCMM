using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamAssetDescriptionIsPermanent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLimited",
                table: "SteamStoreItems");

            migrationBuilder.AddColumn<bool>(
                name: "IsPermanent",
                table: "SteamAssetDescriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPermanent",
                table: "SteamAssetDescriptions");

            migrationBuilder.AddColumn<bool>(
                name: "IsLimited",
                table: "SteamStoreItems",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
