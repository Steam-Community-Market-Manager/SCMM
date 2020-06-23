using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class SteamMarketItemSalesValueSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Last144hrSales",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Last144hrValue",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Last168hrSales",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Last168hrValue",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Last1hrSales",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Last1hrValue",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Last504hrSales",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Last504hrValue",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Last72hrSales",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Last72hrValue",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Last96hrSales",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Last96hrValue",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Last144hrSales",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last144hrValue",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last168hrSales",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last168hrValue",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last1hrSales",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last1hrValue",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last504hrSales",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last504hrValue",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last72hrSales",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last72hrValue",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last96hrSales",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last96hrValue",
                table: "SteamMarketItems");
        }
    }
}
