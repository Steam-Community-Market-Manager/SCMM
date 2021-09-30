using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class CleanUpSteamMarketItemOrders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Demand",
                table: "SteamMarketItems",
                newName: "BuyOrderCount");

            migrationBuilder.RenameColumn(
                name: "BuyAskingPrice",
                table: "SteamMarketItems",
                newName: "BuyOrderHighestPrice");

            migrationBuilder.RenameColumn(
                name: "Supply",
                table: "SteamMarketItems",
                newName: "SellOrderCount");

            migrationBuilder.RenameColumn(
                name: "BuyNowPrice",
                table: "SteamMarketItems",
                newName: "SellOrderLowestPrice");

            migrationBuilder.DropColumn(
                name: "ResellProfit",
                table: "SteamMarketItems");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BuyOrderCount",
                table: "SteamMarketItems",
                newName: "Demand");

            migrationBuilder.RenameColumn(
                name: "BuyOrderHighestPrice",
                table: "SteamMarketItems",
                newName: "ResellProfit");

            migrationBuilder.RenameColumn(
                name: "SellOrderCount",
                table: "SteamMarketItems",
                newName: "Supply");

            migrationBuilder.RenameColumn(
                name: "SellOrderLowestPrice",
                table: "SteamMarketItems",
                newName: "BuyNowPrice");

            migrationBuilder.AddColumn<long>(
                name: "ResellProfit",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
