using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamStoreItemItemStorePricing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyId",
                table: "SteamStoreItemItemStore",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Price",
                table: "SteamStoreItemItemStore",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Prices_Serialised",
                table: "SteamStoreItemItemStore",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamStoreItemItemStore_CurrencyId",
                table: "SteamStoreItemItemStore",
                column: "CurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamStoreItemItemStore_SteamCurrencies_CurrencyId",
                table: "SteamStoreItemItemStore",
                column: "CurrencyId",
                principalTable: "SteamCurrencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
                UPDATE siis 
                SET 
                    siis.CurrencyId = si.CurrencyId,
                    siis.Price = si.Price,
                    siis.Prices_Serialised = si.Prices_Serialised
                FROM [SteamStoreItemItemStore] siis
                    INNER JOIN [SteamStoreItems] si ON si.Id = siis.ItemId
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamStoreItemItemStore_SteamCurrencies_CurrencyId",
                table: "SteamStoreItemItemStore");

            migrationBuilder.DropIndex(
                name: "IX_SteamStoreItemItemStore_CurrencyId",
                table: "SteamStoreItemItemStore");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "SteamStoreItemItemStore");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "SteamStoreItemItemStore");

            migrationBuilder.DropColumn(
                name: "Prices_Serialised",
                table: "SteamStoreItemItemStore");
        }
    }
}
