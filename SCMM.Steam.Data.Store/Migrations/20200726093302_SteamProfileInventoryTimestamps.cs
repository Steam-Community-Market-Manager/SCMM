using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamProfileInventoryTimestamps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUpdatedInventoryOn",
                table: "SteamProfiles",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastViewedInventoryOn",
                table: "SteamProfiles",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdatedInventoryOn",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "LastViewedInventoryOn",
                table: "SteamProfiles");
        }
    }
}
