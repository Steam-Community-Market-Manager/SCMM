using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class SteamMarketItemSaleMetrics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SwingValue",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "TimeOnMarket",
                table: "SteamMarketItems");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "AllTimeLowestValueOn",
                table: "SteamMarketItems",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "AllTimeHighestValueOn",
                table: "SteamMarketItems",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FirstSeenOn",
                table: "SteamMarketItems",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstSeenOn",
                table: "SteamMarketItems");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AllTimeLowestValueOn",
                table: "SteamMarketItems",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "AllTimeHighestValueOn",
                table: "SteamMarketItems",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SwingValue",
                table: "SteamMarketItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TimeOnMarket",
                table: "SteamMarketItems",
                type: "time",
                nullable: true);
        }
    }
}
