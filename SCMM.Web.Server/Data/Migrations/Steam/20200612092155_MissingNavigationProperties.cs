using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class MissingNavigationProperties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamInventoryItems_SteamAssetDescriptions_DescriptionId",
                table: "SteamInventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamInventoryItems_SteamProfiles_SteamProfileId",
                table: "SteamInventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamMarketItems_SteamAssetDescriptions_DescriptionId",
                table: "SteamMarketItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamStoreItems_SteamAssetDescriptions_DescriptionId",
                table: "SteamStoreItems");

            migrationBuilder.DropIndex(
                name: "IX_SteamInventoryItems_SteamProfileId",
                table: "SteamInventoryItems");

            migrationBuilder.DropColumn(
                name: "SteamProfileId",
                table: "SteamInventoryItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "DescriptionId",
                table: "SteamStoreItems",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "DescriptionId",
                table: "SteamMarketItems",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "DescriptionId",
                table: "SteamInventoryItems",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "SteamInventoryItems",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "AppId",
                table: "SteamAssetWorkshopFiles",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("UPDATE [SteamAssetWorkshopFiles] SET [AppId] = (SELECT TOP 1 [Id] FROM [SteamApps])");

            migrationBuilder.AddColumn<Guid>(
                name: "AppId",
                table: "SteamAssetDescriptions",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("UPDATE [SteamAssetDescriptions] SET [AppId] = (SELECT TOP 1 [Id] FROM [SteamApps])");

            migrationBuilder.CreateIndex(
                name: "IX_SteamInventoryItems_OwnerId",
                table: "SteamInventoryItems",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetWorkshopFiles_AppId",
                table: "SteamAssetWorkshopFiles",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_AppId",
                table: "SteamAssetDescriptions",
                column: "AppId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_SteamApps_AppId",
                table: "SteamAssetDescriptions",
                column: "AppId",
                principalTable: "SteamApps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetWorkshopFiles_SteamApps_AppId",
                table: "SteamAssetWorkshopFiles",
                column: "AppId",
                principalTable: "SteamApps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamInventoryItems_SteamAssetDescriptions_DescriptionId",
                table: "SteamInventoryItems",
                column: "DescriptionId",
                principalTable: "SteamAssetDescriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamInventoryItems_SteamProfiles_OwnerId",
                table: "SteamInventoryItems",
                column: "OwnerId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamMarketItems_SteamAssetDescriptions_DescriptionId",
                table: "SteamMarketItems",
                column: "DescriptionId",
                principalTable: "SteamAssetDescriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamStoreItems_SteamAssetDescriptions_DescriptionId",
                table: "SteamStoreItems",
                column: "DescriptionId",
                principalTable: "SteamAssetDescriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_SteamApps_AppId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetWorkshopFiles_SteamApps_AppId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamInventoryItems_SteamAssetDescriptions_DescriptionId",
                table: "SteamInventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamInventoryItems_SteamProfiles_OwnerId",
                table: "SteamInventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamMarketItems_SteamAssetDescriptions_DescriptionId",
                table: "SteamMarketItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamStoreItems_SteamAssetDescriptions_DescriptionId",
                table: "SteamStoreItems");

            migrationBuilder.DropIndex(
                name: "IX_SteamInventoryItems_OwnerId",
                table: "SteamInventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetWorkshopFiles_AppId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_AppId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "SteamInventoryItems");

            migrationBuilder.DropColumn(
                name: "AppId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropColumn(
                name: "AppId",
                table: "SteamAssetDescriptions");

            migrationBuilder.AlterColumn<Guid>(
                name: "DescriptionId",
                table: "SteamStoreItems",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "DescriptionId",
                table: "SteamMarketItems",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "DescriptionId",
                table: "SteamInventoryItems",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SteamProfileId",
                table: "SteamInventoryItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamInventoryItems_SteamProfileId",
                table: "SteamInventoryItems",
                column: "SteamProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamInventoryItems_SteamAssetDescriptions_DescriptionId",
                table: "SteamInventoryItems",
                column: "DescriptionId",
                principalTable: "SteamAssetDescriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamInventoryItems_SteamProfiles_SteamProfileId",
                table: "SteamInventoryItems",
                column: "SteamProfileId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamMarketItems_SteamAssetDescriptions_DescriptionId",
                table: "SteamMarketItems",
                column: "DescriptionId",
                principalTable: "SteamAssetDescriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamStoreItems_SteamAssetDescriptions_DescriptionId",
                table: "SteamStoreItems",
                column: "DescriptionId",
                principalTable: "SteamAssetDescriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
