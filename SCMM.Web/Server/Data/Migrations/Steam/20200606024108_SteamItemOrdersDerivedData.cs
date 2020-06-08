using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class SteamItemOrdersDerivedData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamItemOrder_SteamItems_BuyOrderItemId",
                table: "SteamItemOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamItemOrder_SteamItems_SellOrderItemId",
                table: "SteamItemOrder");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SteamItemOrder",
                table: "SteamItemOrder");

            migrationBuilder.DropIndex(
                name: "IX_SteamItemOrder_BuyOrderItemId",
                table: "SteamItemOrder");

            migrationBuilder.DropIndex(
                name: "IX_SteamItemOrder_SellOrderItemId",
                table: "SteamItemOrder");

            migrationBuilder.DropColumn(
                name: "BuyOrderItemId",
                table: "SteamItemOrder");

            migrationBuilder.DropColumn(
                name: "SellOrderItemId",
                table: "SteamItemOrder");

            migrationBuilder.RenameTable(
                name: "SteamItemOrder",
                newName: "SteamItems_SellOrders");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyId",
                table: "SteamItems",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Demand",
                table: "SteamItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ResellPrice",
                table: "SteamItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ResellProfit",
                table: "SteamItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ResellTax",
                table: "SteamItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SellLowestDelta",
                table: "SteamItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SellLowestPrice",
                table: "SteamItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Supply",
                table: "SteamItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SteamItemId",
                table: "SteamItems_SellOrders",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_SteamItems_SellOrders",
                table: "SteamItems_SellOrders",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "SteamItems_BuyOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Price = table.Column<int>(nullable: false),
                    Quantity = table.Column<int>(nullable: false),
                    SteamItemId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamItems_BuyOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamItems_BuyOrders_SteamItems_SteamItemId",
                        column: x => x.SteamItemId,
                        principalTable: "SteamItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamItems_CurrencyId",
                table: "SteamItems",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamItems_SellOrders_SteamItemId",
                table: "SteamItems_SellOrders",
                column: "SteamItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamItems_BuyOrders_SteamItemId",
                table: "SteamItems_BuyOrders",
                column: "SteamItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamItems_SteamCurrencies_CurrencyId",
                table: "SteamItems",
                column: "CurrencyId",
                principalTable: "SteamCurrencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamItems_SellOrders_SteamItems_SteamItemId",
                table: "SteamItems_SellOrders",
                column: "SteamItemId",
                principalTable: "SteamItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamItems_SteamCurrencies_CurrencyId",
                table: "SteamItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamItems_SellOrders_SteamItems_SteamItemId",
                table: "SteamItems_SellOrders");

            migrationBuilder.DropTable(
                name: "SteamItems_BuyOrders");

            migrationBuilder.DropIndex(
                name: "IX_SteamItems_CurrencyId",
                table: "SteamItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SteamItems_SellOrders",
                table: "SteamItems_SellOrders");

            migrationBuilder.DropIndex(
                name: "IX_SteamItems_SellOrders_SteamItemId",
                table: "SteamItems_SellOrders");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "SteamItems");

            migrationBuilder.DropColumn(
                name: "Demand",
                table: "SteamItems");

            migrationBuilder.DropColumn(
                name: "ResellPrice",
                table: "SteamItems");

            migrationBuilder.DropColumn(
                name: "ResellProfit",
                table: "SteamItems");

            migrationBuilder.DropColumn(
                name: "ResellTax",
                table: "SteamItems");

            migrationBuilder.DropColumn(
                name: "SellLowestDelta",
                table: "SteamItems");

            migrationBuilder.DropColumn(
                name: "SellLowestPrice",
                table: "SteamItems");

            migrationBuilder.DropColumn(
                name: "Supply",
                table: "SteamItems");

            migrationBuilder.DropColumn(
                name: "SteamItemId",
                table: "SteamItems_SellOrders");

            migrationBuilder.RenameTable(
                name: "SteamItems_SellOrders",
                newName: "SteamItemOrder");

            migrationBuilder.AddColumn<Guid>(
                name: "BuyOrderItemId",
                table: "SteamItemOrder",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SellOrderItemId",
                table: "SteamItemOrder",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SteamItemOrder",
                table: "SteamItemOrder",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_SteamItemOrder_BuyOrderItemId",
                table: "SteamItemOrder",
                column: "BuyOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamItemOrder_SellOrderItemId",
                table: "SteamItemOrder",
                column: "SellOrderItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamItemOrder_SteamItems_BuyOrderItemId",
                table: "SteamItemOrder",
                column: "BuyOrderItemId",
                principalTable: "SteamItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamItemOrder_SteamItems_SellOrderItemId",
                table: "SteamItemOrder",
                column: "SellOrderItemId",
                principalTable: "SteamItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
