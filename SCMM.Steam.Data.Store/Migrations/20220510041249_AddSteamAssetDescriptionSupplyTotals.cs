using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamAssetDescriptionSupplyTotals : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BuyPricesTotalSupply",
                table: "SteamMarketItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SellPricesTotalSupply",
                table: "SteamMarketItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "SupplyTotalEstimated",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SupplyTotalInvestorsEstimated",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SupplyTotalInvestorsKnown",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SupplyTotalMarketsKnown",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SupplyTotalOwnersEstimated",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SupplyTotalOwnersKnown",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyPricesTotalSupply",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "SellPricesTotalSupply",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "SupplyTotalEstimated",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "SupplyTotalInvestorsEstimated",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "SupplyTotalInvestorsKnown",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "SupplyTotalMarketsKnown",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "SupplyTotalOwnersEstimated",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "SupplyTotalOwnersKnown",
                table: "SteamAssetDescriptions");
        }
    }
}
