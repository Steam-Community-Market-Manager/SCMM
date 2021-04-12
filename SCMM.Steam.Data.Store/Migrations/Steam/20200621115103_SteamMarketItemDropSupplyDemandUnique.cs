using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations.Steam
{
    public partial class SteamMarketItemDropSupplyDemandUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DemandUnique",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "SupplyUnique",
                table: "SteamMarketItems");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DemandUnique",
                table: "SteamMarketItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SupplyUnique",
                table: "SteamMarketItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
