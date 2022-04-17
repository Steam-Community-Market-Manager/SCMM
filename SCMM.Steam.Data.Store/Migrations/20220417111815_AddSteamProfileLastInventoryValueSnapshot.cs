using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamProfileLastInventoryValueSnapshot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastTotalInventoryItems",
                table: "SteamProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "LastTotalInventoryValue",
                table: "SteamProfiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTotalInventoryItems",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "LastTotalInventoryValue",
                table: "SteamProfiles");
        }
    }
}
