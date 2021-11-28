using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamMarketItemStable24hrOrderPrices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Stable24hrBuyOrderHighestPrice",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Stable24hrSellOrderLowestPrice",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stable24hrBuyOrderHighestPrice",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Stable24hrSellOrderLowestPrice",
                table: "SteamMarketItems");
        }
    }
}
