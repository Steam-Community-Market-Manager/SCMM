using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamMarketItemRolling24hrPriceSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "SteamMarketItemSale",
                newName: "MedianPrice");

            migrationBuilder.AddColumn<string>(
                name: "BuyOrderHighestPriceRolling24hrs_Serialised",
                table: "SteamMarketItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SalesPriceRolling24hrs_Serialised",
                table: "SteamMarketItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SellOrderLowestPriceRolling24hrs_Serialised",
                table: "SteamMarketItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyOrderHighestPriceRolling24hrs_Serialised",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "SalesPriceRolling24hrs_Serialised",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "SellOrderLowestPriceRolling24hrs_Serialised",
                table: "SteamMarketItems");

            migrationBuilder.RenameColumn(
                name: "MedianPrice",
                table: "SteamMarketItemSale",
                newName: "Price");
        }
    }
}
