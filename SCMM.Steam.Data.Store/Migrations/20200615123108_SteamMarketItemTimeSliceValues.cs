using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamMarketItemTimeSliceValues : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentValue",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "OriginalValue",
                table: "SteamMarketItems");

            migrationBuilder.AddColumn<int>(
                name: "First24hrValue",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Last120hrValue",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Last24hrSales",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Last24hrValue",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Last48hrValue",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "First24hrValue",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last120hrValue",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last24hrSales",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last24hrValue",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last48hrValue",
                table: "SteamMarketItems");

            migrationBuilder.AddColumn<int>(
                name: "CurrentValue",
                table: "SteamMarketItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OriginalValue",
                table: "SteamMarketItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
