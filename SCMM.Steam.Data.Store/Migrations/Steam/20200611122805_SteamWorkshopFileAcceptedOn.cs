using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Steam.Data.Store.Migrations.Steam
{
    public partial class SteamWorkshopFileAcceptedOn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstReleasedOn",
                table: "SteamStoreItems");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcceptedOn",
                table: "SteamAssetWorkshopFiles",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedOn",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FirstReleasedOn",
                table: "SteamStoreItems",
                type: "datetimeoffset",
                nullable: true);
        }
    }
}
