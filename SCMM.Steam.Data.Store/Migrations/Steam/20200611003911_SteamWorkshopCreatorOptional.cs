using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations.Steam
{
    public partial class SteamWorkshopCreatorOptional : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetWorkshopFiles_SteamProfiles_CreatorId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetWorkshopFiles_SteamProfiles_CreatorId",
                table: "SteamAssetWorkshopFiles",
                column: "CreatorId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetWorkshopFiles_SteamProfiles_CreatorId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetWorkshopFiles_SteamProfiles_CreatorId",
                table: "SteamAssetWorkshopFiles",
                column: "CreatorId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
