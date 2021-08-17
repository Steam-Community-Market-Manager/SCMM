using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamStoreItemRemoveMarketItemReference : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamInventoryItems_SteamMarketItems_MarketItemId",
                table: "SteamInventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_SteamInventoryItems_MarketItemId",
                table: "SteamInventoryItems");

            migrationBuilder.DropColumn(
                name: "MarketItemId",
                table: "SteamInventoryItems");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MarketItemId",
                table: "SteamInventoryItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamInventoryItems_MarketItemId",
                table: "SteamInventoryItems",
                column: "MarketItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamInventoryItems_SteamMarketItems_MarketItemId",
                table: "SteamInventoryItems",
                column: "MarketItemId",
                principalTable: "SteamMarketItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
