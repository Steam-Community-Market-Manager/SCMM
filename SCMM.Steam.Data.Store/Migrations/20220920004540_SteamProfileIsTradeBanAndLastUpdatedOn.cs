using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamProfileIsTradeBanAndLastUpdatedOn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTradeBanned",
                table: "SteamProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUpdatedOn",
                table: "SteamProfiles",
                type: "datetimeoffset",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTradeBanned",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "LastUpdatedOn",
                table: "SteamProfiles");
        }
    }
}
