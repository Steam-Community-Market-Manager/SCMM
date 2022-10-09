using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamProfileInventoryItemTradableAndMarketable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TradableAndMarketablAfter",
                table: "SteamProfileInventoryItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TradableAndMarketable",
                table: "SteamProfileInventoryItems",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TradableAndMarketablAfter",
                table: "SteamProfileInventoryItems");

            migrationBuilder.DropColumn(
                name: "TradableAndMarketable",
                table: "SteamProfileInventoryItems");
        }
    }
}
