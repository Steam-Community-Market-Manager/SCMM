using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Steam.Data.Store.Migrations.Steam
{
    public partial class SteamMarketItemOrderSalesCascadeDelete : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamMarketItemSale_SteamMarketItems_ItemId",
                table: "SteamMarketItemSale");

            migrationBuilder.DropTable(
                name: "SteamMarketItemOrder");

            migrationBuilder.AlterColumn<Guid>(
                name: "ItemId",
                table: "SteamMarketItemSale",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "SteamMarketItemBuyOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Price = table.Column<int>(nullable: false),
                    Quantity = table.Column<int>(nullable: false),
                    ItemId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamMarketItemBuyOrder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamMarketItemBuyOrder_SteamMarketItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "SteamMarketItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SteamMarketItemSellOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Price = table.Column<int>(nullable: false),
                    Quantity = table.Column<int>(nullable: false),
                    ItemId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamMarketItemSellOrder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamMarketItemSellOrder_SteamMarketItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "SteamMarketItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItemBuyOrder_ItemId",
                table: "SteamMarketItemBuyOrder",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItemSellOrder_ItemId",
                table: "SteamMarketItemSellOrder",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamMarketItemSale_SteamMarketItems_ItemId",
                table: "SteamMarketItemSale",
                column: "ItemId",
                principalTable: "SteamMarketItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamMarketItemSale_SteamMarketItems_ItemId",
                table: "SteamMarketItemSale");

            migrationBuilder.DropTable(
                name: "SteamMarketItemBuyOrder");

            migrationBuilder.DropTable(
                name: "SteamMarketItemSellOrder");

            migrationBuilder.AlterColumn<Guid>(
                name: "ItemId",
                table: "SteamMarketItemSale",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.CreateTable(
                name: "SteamMarketItemOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuyItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Price = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    SellItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamMarketItemOrder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamMarketItemOrder_SteamMarketItems_BuyItemId",
                        column: x => x.BuyItemId,
                        principalTable: "SteamMarketItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SteamMarketItemOrder_SteamMarketItems_SellItemId",
                        column: x => x.SellItemId,
                        principalTable: "SteamMarketItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItemOrder_BuyItemId",
                table: "SteamMarketItemOrder",
                column: "BuyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItemOrder_SellItemId",
                table: "SteamMarketItemOrder",
                column: "SellItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamMarketItemSale_SteamMarketItems_ItemId",
                table: "SteamMarketItemSale",
                column: "ItemId",
                principalTable: "SteamMarketItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
