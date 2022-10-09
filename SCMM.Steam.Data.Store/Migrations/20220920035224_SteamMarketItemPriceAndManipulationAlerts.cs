using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamMarketItemPriceAndManipulationAlerts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastManipulationAlertOn",
                table: "SteamMarketItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastPriceAlertOn",
                table: "SteamMarketItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManipulationReason",
                table: "SteamMarketItems",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastManipulationAlertOn",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "LastPriceAlertOn",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "ManipulationReason",
                table: "SteamMarketItems");
        }
    }
}
