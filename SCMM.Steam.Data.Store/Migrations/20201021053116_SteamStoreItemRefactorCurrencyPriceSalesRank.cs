using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamStoreItemRefactorCurrencyPriceSalesRank : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoreRankPosition",
                table: "SteamStoreItems");

            migrationBuilder.DropColumn(
                name: "StoreRankTotal",
                table: "SteamStoreItems");

            migrationBuilder.DropColumn(
                name: "StoreRankGraph_Serialised",
                table: "SteamStoreItems");

            migrationBuilder.RenameColumn(
                name: "StorePrices_Serialised",
                table: "SteamStoreItems",
                newName: "Prices_Serialised");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyId",
                table: "SteamStoreItems",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Price",
                table: "SteamStoreItems",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "Index",
                table: "SteamStoreItemItemStore",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IndexGraph_Serialised",
                table: "SteamStoreItemItemStore",
                nullable: true);

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

        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "Price",
                table: "SteamStoreItems");

            migrationBuilder.DropColumn(
                name: "Index",
                table: "SteamStoreItemItemStore");

            migrationBuilder.DropColumn(
                name: "IndexGraph_Serialised",
                table: "SteamStoreItemItemStore");

            migrationBuilder.RenameColumn(
                name: "Prices_Serialised",
                table: "SteamStoreItems",
                newName: "StorePrices_Serialised");

            migrationBuilder.AddColumn<int>(
                name: "StoreRankPosition",
                table: "SteamStoreItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StoreRankTotal",
                table: "SteamStoreItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StoreRankGraph_Serialised",
                table: "SteamStoreItems",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
