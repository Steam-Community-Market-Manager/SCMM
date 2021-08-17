using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamStoreItemMultiCurrency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamStoreItems_SteamCurrencies_CurrencyId",
                table: "SteamStoreItems");

            migrationBuilder.DropIndex(
                name: "IX_SteamStoreItems_CurrencyId",
                table: "SteamStoreItems");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "SteamStoreItems");

            migrationBuilder.DropColumn(
                name: "StorePrice",
                table: "SteamStoreItems");

            migrationBuilder.AddColumn<string>(
                name: "StorePrices_Serialised",
                table: "SteamStoreItems",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StorePrices_Serialised",
                table: "SteamStoreItems");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyId",
                table: "SteamStoreItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StorePrice",
                table: "SteamStoreItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_SteamStoreItems_CurrencyId",
                table: "SteamStoreItems",
                column: "CurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamStoreItems_SteamCurrencies_CurrencyId",
                table: "SteamStoreItems",
                column: "CurrencyId",
                principalTable: "SteamCurrencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
