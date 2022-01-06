using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamMarketItemBuySellLaterPriceRefactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ResellTax",
                table: "SteamMarketItems",
                newName: "SellLaterTax");

            migrationBuilder.RenameColumn(
                name: "ResellPrice",
                table: "SteamMarketItems",
                newName: "SellLaterPrice");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SellLaterTax",
                table: "SteamMarketItems",
                newName: "ResellTax");

            migrationBuilder.RenameColumn(
                name: "SellLaterPrice",
                table: "SteamMarketItems",
                newName: "ResellPrice");
        }
    }
}
