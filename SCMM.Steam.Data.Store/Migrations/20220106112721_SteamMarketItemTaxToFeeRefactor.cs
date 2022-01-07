using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamMarketItemTaxToFeeRefactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SellNowTax",
                table: "SteamMarketItems",
                newName: "SellNowFee");

            migrationBuilder.RenameColumn(
                name: "SellLaterTax",
                table: "SteamMarketItems",
                newName: "SellLaterFee");

            migrationBuilder.RenameColumn(
                name: "BuyNowTax",
                table: "SteamMarketItems",
                newName: "BuyNowFee");

            migrationBuilder.RenameColumn(
                name: "BuyLaterTax",
                table: "SteamMarketItems",
                newName: "BuyLaterFee");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SellNowFee",
                table: "SteamMarketItems",
                newName: "SellNowTax");

            migrationBuilder.RenameColumn(
                name: "SellLaterFee",
                table: "SteamMarketItems",
                newName: "SellLaterTax");

            migrationBuilder.RenameColumn(
                name: "BuyNowFee",
                table: "SteamMarketItems",
                newName: "BuyNowTax");

            migrationBuilder.RenameColumn(
                name: "BuyLaterFee",
                table: "SteamMarketItems",
                newName: "BuyLaterTax");
        }
    }
}
