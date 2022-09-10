using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamMarketItemBuySellPriceSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Prices_Serialised",
                table: "SteamMarketItems",
                newName: "BuyPrices_Serialised");

            migrationBuilder.AddColumn<byte>(
                name: "BuyLaterFrom",
                table: "SteamMarketItems",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<long>(
                name: "BuyLaterPrice",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "BuyLaterTax",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "BuyNowTax",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "SellPrices_Serialised",
                table: "SteamMarketItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "SellLaterTo",
                table: "SteamMarketItems",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<long>(
                name: "SellNowPrice",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "SellNowTax",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<byte>(
                name: "SellNowTo",
                table: "SteamMarketItems",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyLaterFrom",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "BuyLaterPrice",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "BuyLaterTax",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "BuyNowTax",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "SellPrices_Serialised",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "SellLaterTo",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "SellNowPrice",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "SellNowTax",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "SellNowTo",
                table: "SteamMarketItems");

            migrationBuilder.RenameColumn(
                name: "BuyPrices_Serialised",
                table: "SteamMarketItems",
                newName: "Prices_Serialised");
        }
    }
}
