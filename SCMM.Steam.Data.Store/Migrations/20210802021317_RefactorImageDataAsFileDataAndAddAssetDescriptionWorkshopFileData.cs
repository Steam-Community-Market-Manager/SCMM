using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RefactorImageDataAsFileDataAndAddAssetDescriptionWorkshopFileData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamApps_ImageData_IconId",
                table: "SteamApps");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamApps_ImageData_IconLargeId",
                table: "SteamApps");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_ImageData_IconId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_ImageData_IconLargeId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_ImageData_PreviewId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetWorkshopFiles_ImageData_ImageId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamItemStores_ImageData_ItemsThumbnailId",
                table: "SteamItemStores");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamProfiles_ImageData_AvatarId",
                table: "SteamProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamProfiles_ImageData_AvatarLargeId",
                table: "SteamProfiles");

            migrationBuilder.RenameTable(
                name: "ImageData",
                newName: "FileData");

            migrationBuilder.RenameIndex(
                name: "PK_ImageData",
                newName: "PK_FileData",
                table: "FileData");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkshopFileDataId",
                table: "SteamAssetDescriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_WorkshopFileDataId",
                table: "SteamAssetDescriptions",
                column: "WorkshopFileDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamApps_FileData_IconId",
                table: "SteamApps",
                column: "IconId",
                principalTable: "FileData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamApps_FileData_IconLargeId",
                table: "SteamApps",
                column: "IconLargeId",
                principalTable: "FileData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_FileData_IconId",
                table: "SteamAssetDescriptions",
                column: "IconId",
                principalTable: "FileData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_FileData_IconLargeId",
                table: "SteamAssetDescriptions",
                column: "IconLargeId",
                principalTable: "FileData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_FileData_PreviewId",
                table: "SteamAssetDescriptions",
                column: "PreviewId",
                principalTable: "FileData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_FileData_WorkshopFileDataId",
                table: "SteamAssetDescriptions",
                column: "WorkshopFileDataId",
                principalTable: "FileData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetWorkshopFiles_FileData_ImageId",
                table: "SteamAssetWorkshopFiles",
                column: "ImageId",
                principalTable: "FileData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamItemStores_FileData_ItemsThumbnailId",
                table: "SteamItemStores",
                column: "ItemsThumbnailId",
                principalTable: "FileData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamProfiles_FileData_AvatarId",
                table: "SteamProfiles",
                column: "AvatarId",
                principalTable: "FileData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamProfiles_FileData_AvatarLargeId",
                table: "SteamProfiles",
                column: "AvatarLargeId",
                principalTable: "FileData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamApps_FileData_IconId",
                table: "SteamApps");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamApps_FileData_IconLargeId",
                table: "SteamApps");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_FileData_IconId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_FileData_IconLargeId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_FileData_PreviewId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_FileData_WorkshopFileDataId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetWorkshopFiles_FileData_ImageId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamItemStores_FileData_ItemsThumbnailId",
                table: "SteamItemStores");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamProfiles_FileData_AvatarId",
                table: "SteamProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamProfiles_FileData_AvatarLargeId",
                table: "SteamProfiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_WorkshopFileDataId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "WorkshopFileDataId",
                table: "SteamAssetDescriptions");

            migrationBuilder.RenameIndex(
                name: "PK_FileData",
                newName: "PK_ImageData",
                table: "FileData");

            migrationBuilder.RenameTable(
                name: "FileData",
                newName: "ImageData");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamApps_ImageData_IconId",
                table: "SteamApps",
                column: "IconId",
                principalTable: "ImageData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamApps_ImageData_IconLargeId",
                table: "SteamApps",
                column: "IconLargeId",
                principalTable: "ImageData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_ImageData_IconId",
                table: "SteamAssetDescriptions",
                column: "IconId",
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
                name: "FK_SteamAssetDescriptions_ImageData_PreviewId",
                table: "SteamAssetDescriptions",
                column: "PreviewId",
                principalTable: "ImageData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetWorkshopFiles_ImageData_ImageId",
                table: "SteamAssetWorkshopFiles",
                column: "ImageId",
                principalTable: "ImageData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamItemStores_ImageData_ItemsThumbnailId",
                table: "SteamItemStores",
                column: "ItemsThumbnailId",
                principalTable: "ImageData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamProfiles_ImageData_AvatarId",
                table: "SteamProfiles",
                column: "AvatarId",
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
    }
}
