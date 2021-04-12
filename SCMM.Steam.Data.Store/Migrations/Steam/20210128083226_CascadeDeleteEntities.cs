using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Steam.Data.Store.Migrations.Steam
{
    public partial class CascadeDeleteEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetWorkshopFiles_SteamProfiles_CreatorId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.AlterColumn<Guid>(
                name: "SteamAppId",
                table: "SteamAssetFilter",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetWorkshopFiles_SteamProfiles_CreatorId",
                table: "SteamAssetWorkshopFiles",
                column: "CreatorId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetWorkshopFiles_SteamProfiles_CreatorId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.AlterColumn<Guid>(
                name: "SteamAppId",
                table: "SteamAssetFilter",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetWorkshopFiles_SteamProfiles_CreatorId",
                table: "SteamAssetWorkshopFiles",
                column: "CreatorId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
