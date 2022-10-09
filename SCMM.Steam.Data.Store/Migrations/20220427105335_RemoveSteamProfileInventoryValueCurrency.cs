using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RemoveSteamProfileInventoryValueCurrency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamProfileInventoryValues_SteamCurrencies_CurrencyId",
                table: "SteamProfileInventoryValues");

            migrationBuilder.DropIndex(
                name: "IX_SteamProfileInventoryValues_CurrencyId",
                table: "SteamProfileInventoryValues");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "SteamProfileInventoryValues");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyId",
                table: "SteamProfileInventoryValues",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfileInventoryValues_CurrencyId",
                table: "SteamProfileInventoryValues",
                column: "CurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamProfileInventoryValues_SteamCurrencies_CurrencyId",
                table: "SteamProfileInventoryValues",
                column: "CurrencyId",
                principalTable: "SteamCurrencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
