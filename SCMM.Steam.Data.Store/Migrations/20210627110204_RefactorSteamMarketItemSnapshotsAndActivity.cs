using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RefactorSteamMarketItemSnapshotsAndActivity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Last336hrSales",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last336hrValue",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last504hrSales",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Last504hrValue",
                table: "SteamMarketItems");

            migrationBuilder.Sql(@"
                DELETE FROM [SteamMarketItemActivity]
            ");

            migrationBuilder.RenameColumn(
                name: "Movement",
                table: "SteamMarketItemActivity",
                newName: "Quantity");

            migrationBuilder.AddColumn<string>(
                name: "BuyerAvatarUrl",
                table: "SteamMarketItemActivity",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerName",
                table: "SteamMarketItemActivity",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyId",
                table: "SteamMarketItemActivity",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DescriptionId",
                table: "SteamMarketItemActivity",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Price",
                table: "SteamMarketItemActivity",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "SellerAvatarUrl",
                table: "SteamMarketItemActivity",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerName",
                table: "SteamMarketItemActivity",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "SteamMarketItemActivity",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItemActivity_CurrencyId",
                table: "SteamMarketItemActivity",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItemActivity_DescriptionId",
                table: "SteamMarketItemActivity",
                column: "DescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItemActivity_Timestamp_DescriptionId_Type_Price_Quantity_BuyerName_SellerName",
                table: "SteamMarketItemActivity",
                columns: new[] { "Timestamp", "DescriptionId", "Type", "Price", "Quantity", "BuyerName", "SellerName" },
                unique: true,
                filter: "[DescriptionId] IS NOT NULL AND [BuyerName] IS NOT NULL AND [SellerName] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamMarketItemActivity_SteamAssetDescriptions_DescriptionId",
                table: "SteamMarketItemActivity",
                column: "DescriptionId",
                principalTable: "SteamAssetDescriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamMarketItemActivity_SteamCurrencies_CurrencyId",
                table: "SteamMarketItemActivity",
                column: "CurrencyId",
                principalTable: "SteamCurrencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamMarketItemActivity_SteamAssetDescriptions_DescriptionId",
                table: "SteamMarketItemActivity");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamMarketItemActivity_SteamCurrencies_CurrencyId",
                table: "SteamMarketItemActivity");

            migrationBuilder.DropIndex(
                name: "IX_SteamMarketItemActivity_CurrencyId",
                table: "SteamMarketItemActivity");

            migrationBuilder.DropIndex(
                name: "IX_SteamMarketItemActivity_DescriptionId",
                table: "SteamMarketItemActivity");

            migrationBuilder.DropIndex(
                name: "IX_SteamMarketItemActivity_Timestamp_DescriptionId_Type_Price_Quantity_BuyerName_SellerName",
                table: "SteamMarketItemActivity");

            migrationBuilder.DropColumn(
                name: "BuyerAvatarUrl",
                table: "SteamMarketItemActivity");

            migrationBuilder.DropColumn(
                name: "BuyerName",
                table: "SteamMarketItemActivity");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "SteamMarketItemActivity");

            migrationBuilder.DropColumn(
                name: "DescriptionId",
                table: "SteamMarketItemActivity");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "SteamMarketItemActivity");

            migrationBuilder.DropColumn(
                name: "SellerAvatarUrl",
                table: "SteamMarketItemActivity");

            migrationBuilder.DropColumn(
                name: "SellerName",
                table: "SteamMarketItemActivity");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "SteamMarketItemActivity");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "SteamMarketItemActivity",
                newName: "Movement");

            migrationBuilder.AddColumn<long>(
                name: "Last336hrSales",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Last336hrValue",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Last504hrSales",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Last504hrValue",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
