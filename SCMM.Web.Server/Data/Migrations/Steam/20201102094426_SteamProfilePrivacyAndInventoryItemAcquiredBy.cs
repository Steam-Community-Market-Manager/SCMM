using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class SteamProfilePrivacyAndInventoryItemAcquiredBy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "Privacy",
                table: "SteamProfiles",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "AcquiredBy",
                table: "SteamProfileInventoryItems",
                nullable: false,
                defaultValue: (byte)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Privacy",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "AcquiredBy",
                table: "SteamProfileInventoryItems");
        }
    }
}
