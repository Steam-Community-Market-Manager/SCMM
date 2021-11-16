using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AllowSteamItemStoresWithNullStartDates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SteamItemStores_AppId_Start_End",
                table: "SteamItemStores");

            migrationBuilder.AddColumn<bool>(
                name: "IsLimited",
                table: "SteamStoreItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Start",
                table: "SteamItemStores",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SteamItemStores",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamItemStores_AppId_Start_End_Name",
                table: "SteamItemStores",
                columns: new[] { "AppId", "Start", "End", "Name" },
                unique: true,
                filter: "[Start] IS NOT NULL AND [End] IS NOT NULL AND [Name] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SteamItemStores_AppId_Start_End_Name",
                table: "SteamItemStores");

            migrationBuilder.DropColumn(
                name: "IsLimited",
                table: "SteamStoreItems");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Start",
                table: "SteamItemStores",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SteamItemStores",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamItemStores_AppId_Start_End",
                table: "SteamItemStores",
                columns: new[] { "AppId", "Start", "End" },
                unique: true,
                filter: "[End] IS NOT NULL");
        }
    }
}
