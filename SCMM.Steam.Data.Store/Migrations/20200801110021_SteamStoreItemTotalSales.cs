using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamStoreItemTotalSales : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StoreRankPosition",
                table: "SteamStoreItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StoreRankTotal",
                table: "SteamStoreItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalSalesMax",
                table: "SteamStoreItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalSalesMin",
                table: "SteamStoreItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TotalSalesGraph_Serialised",
                table: "SteamStoreItems",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoreRankPosition",
                table: "SteamStoreItems");

            migrationBuilder.DropColumn(
                name: "StoreRankTotal",
                table: "SteamStoreItems");

            migrationBuilder.DropColumn(
                name: "TotalSalesMax",
                table: "SteamStoreItems");

            migrationBuilder.DropColumn(
                name: "TotalSalesMin",
                table: "SteamStoreItems");

            migrationBuilder.DropColumn(
                name: "TotalSalesGraph_Serialised",
                table: "SteamStoreItems");
        }
    }
}
