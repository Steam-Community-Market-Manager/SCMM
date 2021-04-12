using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations.Steam
{
    public partial class SteamMarketItemSalesSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DemandUnique",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Last120hrSales",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Last336hrSales",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Last48hrSales",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SupplyUnique",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DemandUnique",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last120hrSales",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last336hrSales",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last48hrSales",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "SupplyUnique",
                table: "SteamMarketItems");
        }
    }
}
