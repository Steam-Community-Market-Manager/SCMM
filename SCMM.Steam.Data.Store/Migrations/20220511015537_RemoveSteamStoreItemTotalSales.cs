using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RemoveSteamStoreItemTotalSales : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalSalesMax",
                table: "SteamStoreItems");

            migrationBuilder.DropColumn(
                name: "TotalSalesMin",
                table: "SteamStoreItems");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TotalSalesMax",
                table: "SteamStoreItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TotalSalesMin",
                table: "SteamStoreItems",
                type: "bigint",
                nullable: true);
        }
    }
}
