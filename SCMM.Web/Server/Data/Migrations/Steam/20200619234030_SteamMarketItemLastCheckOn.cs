using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class SteamMarketItemLastCheckOn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCheckedOn",
                table: "SteamMarketItems");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastCheckedOrdersOn",
                table: "SteamMarketItems",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastCheckedSalesOn",
                table: "SteamMarketItems",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCheckedOrdersOn",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "LastCheckedSalesOn",
                table: "SteamMarketItems");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastCheckedOn",
                table: "SteamMarketItems",
                type: "datetimeoffset",
                nullable: true);
        }
    }
}
