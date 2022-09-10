using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamAppFeatures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoreTypes",
                table: "SteamApps");

            migrationBuilder.AddColumn<long>(
                name: "Features",
                table: "SteamApps",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Features",
                table: "SteamApps");

            migrationBuilder.AddColumn<byte>(
                name: "StoreTypes",
                table: "SteamApps",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);
        }
    }
}
