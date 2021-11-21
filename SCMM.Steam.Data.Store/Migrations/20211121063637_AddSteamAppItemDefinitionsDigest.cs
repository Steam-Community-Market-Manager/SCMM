using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamAppItemDefinitionsDigest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ItemDefinitionsDigest",
                table: "SteamApps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TimeUpdated",
                table: "SteamApps",
                type: "datetimeoffset",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemDefinitionsDigest",
                table: "SteamApps");

            migrationBuilder.DropColumn(
                name: "TimeUpdated",
                table: "SteamApps");
        }
    }
}
