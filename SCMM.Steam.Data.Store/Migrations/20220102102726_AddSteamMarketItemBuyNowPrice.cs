using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamMarketItemBuyNowPrice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCheckedThirdPartyPricesOn",
                table: "SteamMarketItems");

            migrationBuilder.AddColumn<byte>(
                name: "BuyNowFrom",
                table: "SteamMarketItems",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<long>(
                name: "BuyNowPrice",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyNowFrom",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "BuyNowPrice",
                table: "SteamMarketItems");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastCheckedThirdPartyPricesOn",
                table: "SteamMarketItems",
                type: "datetimeoffset",
                nullable: true);
        }
    }
}
