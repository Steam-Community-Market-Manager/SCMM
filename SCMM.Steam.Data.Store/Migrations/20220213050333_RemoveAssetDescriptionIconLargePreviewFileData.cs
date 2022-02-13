using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RemoveAssetDescriptionIconLargePreviewFileData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_FileData_IconLargeId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_FileData_PreviewId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_IconLargeId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_PreviewId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "IconLargeId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "PreviewId",
                table: "SteamAssetDescriptions");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IconLargeId",
                table: "SteamAssetDescriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreviewId",
                table: "SteamAssetDescriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_IconLargeId",
                table: "SteamAssetDescriptions",
                column: "IconLargeId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_PreviewId",
                table: "SteamAssetDescriptions",
                column: "PreviewId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_FileData_IconLargeId",
                table: "SteamAssetDescriptions",
                column: "IconLargeId",
                principalTable: "FileData",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_FileData_PreviewId",
                table: "SteamAssetDescriptions",
                column: "PreviewId",
                principalTable: "FileData",
                principalColumn: "Id");
        }
    }
}
