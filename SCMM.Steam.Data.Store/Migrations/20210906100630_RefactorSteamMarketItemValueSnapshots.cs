using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RefactorSteamMarketItemValueSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BuyNowPriceDelta",
                table: "SteamMarketItems",
                newName: "Open24hrValue");

            migrationBuilder.AddColumn<long>(
                name: "First24hrSales",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql(@"
                UPDATE m 
                SET m.Open24hrValue = ISNULL((SELECT TOP 1 [Price] FROM [SteamMarketItemSale] WHERE [ItemId] = m.id AND [Timestamp] >= DATEADD(DAY, DATEDIFF(DAY, 0, GETDATE()),0) ORDER BY [Timestamp] ASC), 0)
                FROM [SteamMarketItems] m
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "First24hrSales",
                table: "SteamMarketItems");

            migrationBuilder.RenameColumn(
                name: "Open24hrValue",
                table: "SteamMarketItems",
                newName: "BuyNowPriceDelta");
        }
    }
}
