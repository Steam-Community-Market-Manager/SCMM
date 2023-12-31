﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamItemStoreItemsThumbnail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ItemsThumbnailId",
                table: "SteamItemStores",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamItemStores_ItemsThumbnailId",
                table: "SteamItemStores",
                column: "ItemsThumbnailId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamItemStores_ImageData_ItemsThumbnailId",
                table: "SteamItemStores",
                column: "ItemsThumbnailId",
                principalTable: "ImageData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamItemStores_ImageData_ItemsThumbnailId",
                table: "SteamItemStores");

            migrationBuilder.DropIndex(
                name: "IX_SteamItemStores_ItemsThumbnailId",
                table: "SteamItemStores");

            migrationBuilder.DropColumn(
                name: "ItemsThumbnailId",
                table: "SteamItemStores");
        }
    }
}
