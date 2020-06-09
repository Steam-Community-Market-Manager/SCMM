using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class LastCheckedOn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastChecked",
                table: "SteamMarketItems");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastCheckedOn",
                table: "SteamMarketItems",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastCheckedOn",
                table: "SteamAssetWorkshopFiles",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastCheckedOn",
                table: "SteamAssetDescriptions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCheckedOn",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "LastCheckedOn",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropColumn(
                name: "LastCheckedOn",
                table: "SteamAssetDescriptions");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastChecked",
                table: "SteamMarketItems",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
