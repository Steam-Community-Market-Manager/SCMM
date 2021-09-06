using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamMarketItemLastSale : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSaleOn",
                table: "SteamMarketItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LastSaleValue",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE m SET 
                    m.LastSaleValue = (SELECT TOP 1 [Price] FROM [SteamMarketItemSale] WHERE [ItemId] = m.id ORDER BY [Timestamp] DESC),
                    m.LastSaleOn = (SELECT TOP 1 [Timestamp] FROM [SteamMarketItemSale] WHERE [ItemId] = m.id ORDER BY [Timestamp] DESC)
                FROM [SteamMarketItems] m
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSaleOn",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "LastSaleValue",
                table: "SteamMarketItems");
        }
    }
}
