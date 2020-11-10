using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class RenameSteamInventoryItemOwnerAsProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamInventoryItems_SteamProfiles_OwnerId",
                table: "SteamInventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_SteamInventoryItems_OwnerId",
                table: "SteamInventoryItems");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "SteamInventoryItems",
                newName: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamInventoryItems_ProfileId",
                table: "SteamInventoryItems",
                column: "ProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamInventoryItems_SteamProfiles_ProfileId",
                table: "SteamInventoryItems",
                column: "ProfileId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamInventoryItems_SteamProfiles_ProfileId",
                table: "SteamInventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_SteamInventoryItems_ProfileId",
                table: "SteamInventoryItems");

            migrationBuilder.RenameColumn(
                name: "ProfileId",
                table: "SteamInventoryItems",
                newName: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamInventoryItems_OwnerId",
                table: "SteamInventoryItems",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamInventoryItems_SteamProfiles_OwnerId",
                table: "SteamInventoryItems",
                column: "OwnerId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
