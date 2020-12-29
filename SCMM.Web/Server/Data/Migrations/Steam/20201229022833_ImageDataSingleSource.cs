using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class ImageDataSingleSource : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Value",
                table: "ImageData");

            migrationBuilder.RenameColumn(
                name: "ValueLarge",
                table: "ImageData",
                newName: "Data");

            migrationBuilder.RenameColumn(
                name: "MineType",
                table: "ImageData",
                newName: "Source");

            migrationBuilder.AddColumn<Guid>(
                name: "AvatarLargeId",
                table: "SteamProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IconLargeId",
                table: "SteamAssetDescriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IconLargeId",
                table: "SteamApps",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MimeType",
                table: "ImageData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfiles_AvatarLargeId",
                table: "SteamProfiles",
                column: "AvatarLargeId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_IconLargeId",
                table: "SteamAssetDescriptions",
                column: "IconLargeId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamApps_IconLargeId",
                table: "SteamApps",
                column: "IconLargeId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamApps_ImageData_IconLargeId",
                table: "SteamApps",
                column: "IconLargeId",
                principalTable: "ImageData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_ImageData_IconLargeId",
                table: "SteamAssetDescriptions",
                column: "IconLargeId",
                principalTable: "ImageData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamProfiles_ImageData_AvatarLargeId",
                table: "SteamProfiles",
                column: "AvatarLargeId",
                principalTable: "ImageData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamApps_ImageData_IconLargeId",
                table: "SteamApps");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_ImageData_IconLargeId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamProfiles_ImageData_AvatarLargeId",
                table: "SteamProfiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamProfiles_AvatarLargeId",
                table: "SteamProfiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_IconLargeId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropIndex(
                name: "IX_SteamApps_IconLargeId",
                table: "SteamApps");

            migrationBuilder.DropColumn(
                name: "AvatarLargeId",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "IconLargeId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "IconLargeId",
                table: "SteamApps");

            migrationBuilder.DropColumn(
                name: "MimeType",
                table: "ImageData");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "ImageData",
                newName: "MineType");

            migrationBuilder.RenameColumn(
                name: "Data",
                table: "ImageData",
                newName: "ValueLarge");

            migrationBuilder.AddColumn<byte[]>(
                name: "Value",
                table: "ImageData",
                type: "varbinary(max)",
                nullable: true);
        }
    }
}
