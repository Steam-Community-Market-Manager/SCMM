using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamMarketItemOrderHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BuyOrderCumulativePrice",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "SellOrderCumulativePrice",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "SteamMarketItemOrderSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    BuyCount = table.Column<int>(type: "int", nullable: false),
                    BuyCumulativePrice = table.Column<long>(type: "bigint", nullable: false),
                    BuyHighestPrice = table.Column<long>(type: "bigint", nullable: false),
                    SellCount = table.Column<int>(type: "int", nullable: false),
                    SellCumulativePrice = table.Column<long>(type: "bigint", nullable: false),
                    SellLowestPrice = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamMarketItemOrderSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamMarketItemOrderSummaries_SteamMarketItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "SteamMarketItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItemOrderSummaries_ItemId",
                table: "SteamMarketItemOrderSummaries",
                column: "ItemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamMarketItemOrderSummaries");

            migrationBuilder.DropColumn(
                name: "BuyOrderCumulativePrice",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "SellOrderCumulativePrice",
                table: "SteamMarketItems");
        }
    }
}
