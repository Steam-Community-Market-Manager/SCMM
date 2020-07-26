using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Web.Server.Data.Migrations.Steam
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
