using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class SteamMarketItemRemoveOwns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamMarketItems_SellOrders_SteamMarketItems_SteamMarketItemId",
                table: "SteamMarketItems_SellOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamMarketItemSale_SteamMarketItems_SteamMarketItemId",
                table: "SteamMarketItemSale");

            migrationBuilder.DropTable(
                name: "SteamMarketItems_BuyOrders");

            migrationBuilder.DropIndex(
                name: "IX_SteamMarketItemSale_SteamMarketItemId",
                table: "SteamMarketItemSale");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SteamMarketItems_SellOrders",
                table: "SteamMarketItems_SellOrders");

            migrationBuilder.DropIndex(
                name: "IX_SteamMarketItems_SellOrders_SteamMarketItemId",
                table: "SteamMarketItems_SellOrders");

            migrationBuilder.DropColumn(
                name: "SteamMarketItemId",
                table: "SteamMarketItemSale");

            migrationBuilder.DropColumn(
                name: "SteamMarketItemId",
                table: "SteamMarketItems_SellOrders");

            migrationBuilder.RenameTable(
                name: "SteamMarketItems_SellOrders",
                newName: "SteamMarketItemOrder");

            migrationBuilder.Sql("DELETE FROM [SteamMarketItemOrder]");
            migrationBuilder.Sql("DELETE FROM [SteamMarketItemSale]");

            migrationBuilder.AddColumn<Guid>(
                name: "ItemId",
                table: "SteamMarketItemSale",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BuyItemId",
                table: "SteamMarketItemOrder",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SellItemId",
                table: "SteamMarketItemOrder",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SteamMarketItemOrder",
                table: "SteamMarketItemOrder",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItemSale_ItemId",
                table: "SteamMarketItemSale",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItemOrder_BuyItemId",
                table: "SteamMarketItemOrder",
                column: "BuyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItemOrder_SellItemId",
                table: "SteamMarketItemOrder",
                column: "SellItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamMarketItemOrder_SteamMarketItems_BuyItemId",
                table: "SteamMarketItemOrder",
                column: "BuyItemId",
                principalTable: "SteamMarketItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamMarketItemOrder_SteamMarketItems_SellItemId",
                table: "SteamMarketItemOrder",
                column: "SellItemId",
                principalTable: "SteamMarketItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamMarketItemSale_SteamMarketItems_ItemId",
                table: "SteamMarketItemSale",
                column: "ItemId",
                principalTable: "SteamMarketItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamMarketItemOrder_SteamMarketItems_BuyItemId",
                table: "SteamMarketItemOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamMarketItemOrder_SteamMarketItems_SellItemId",
                table: "SteamMarketItemOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamMarketItemSale_SteamMarketItems_ItemId",
                table: "SteamMarketItemSale");

            migrationBuilder.DropIndex(
                name: "IX_SteamMarketItemSale_ItemId",
                table: "SteamMarketItemSale");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SteamMarketItemOrder",
                table: "SteamMarketItemOrder");

            migrationBuilder.DropIndex(
                name: "IX_SteamMarketItemOrder_BuyItemId",
                table: "SteamMarketItemOrder");

            migrationBuilder.DropIndex(
                name: "IX_SteamMarketItemOrder_SellItemId",
                table: "SteamMarketItemOrder");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "SteamMarketItemSale");

            migrationBuilder.DropColumn(
                name: "BuyItemId",
                table: "SteamMarketItemOrder");

            migrationBuilder.DropColumn(
                name: "SellItemId",
                table: "SteamMarketItemOrder");

            migrationBuilder.RenameTable(
                name: "SteamMarketItemOrder",
                newName: "SteamMarketItems_SellOrders");

            migrationBuilder.AddColumn<Guid>(
                name: "SteamMarketItemId",
                table: "SteamMarketItemSale",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SteamMarketItemId",
                table: "SteamMarketItems_SellOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_SteamMarketItems_SellOrders",
                table: "SteamMarketItems_SellOrders",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "SteamMarketItems_BuyOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Price = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    SteamMarketItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamMarketItems_BuyOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamMarketItems_BuyOrders_SteamMarketItems_SteamMarketItemId",
                        column: x => x.SteamMarketItemId,
                        principalTable: "SteamMarketItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItemSale_SteamMarketItemId",
                table: "SteamMarketItemSale",
                column: "SteamMarketItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItems_SellOrders_SteamMarketItemId",
                table: "SteamMarketItems_SellOrders",
                column: "SteamMarketItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItems_BuyOrders_SteamMarketItemId",
                table: "SteamMarketItems_BuyOrders",
                column: "SteamMarketItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamMarketItems_SellOrders_SteamMarketItems_SteamMarketItemId",
                table: "SteamMarketItems_SellOrders",
                column: "SteamMarketItemId",
                principalTable: "SteamMarketItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamMarketItemSale_SteamMarketItems_SteamMarketItemId",
                table: "SteamMarketItemSale",
                column: "SteamMarketItemId",
                principalTable: "SteamMarketItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
