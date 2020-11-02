using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class RenameSteamInventoryItemAsSteamProfileInventoryItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamInventoryItems_SteamApps_AppId",
                table: "SteamInventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamInventoryItems_SteamCurrencies_CurrencyId",
                table: "SteamInventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamInventoryItems_SteamAssetDescriptions_DescriptionId",
                table: "SteamInventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamInventoryItems_SteamProfiles_ProfileId",
                table: "SteamInventoryItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SteamInventoryItems",
                table: "SteamInventoryItems");

            migrationBuilder.RenameTable(
                name: "SteamInventoryItems",
                newName: "SteamProfileInventoryItems");

            migrationBuilder.RenameIndex(
                name: "IX_SteamInventoryItems_ProfileId",
                table: "SteamProfileInventoryItems",
                newName: "IX_SteamProfileInventoryItems_ProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_SteamInventoryItems_DescriptionId",
                table: "SteamProfileInventoryItems",
                newName: "IX_SteamProfileInventoryItems_DescriptionId");

            migrationBuilder.RenameIndex(
                name: "IX_SteamInventoryItems_CurrencyId",
                table: "SteamProfileInventoryItems",
                newName: "IX_SteamProfileInventoryItems_CurrencyId");

            migrationBuilder.RenameIndex(
                name: "IX_SteamInventoryItems_AppId",
                table: "SteamProfileInventoryItems",
                newName: "IX_SteamProfileInventoryItems_AppId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SteamProfileInventoryItems",
                table: "SteamProfileInventoryItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamProfileInventoryItems_SteamApps_AppId",
                table: "SteamProfileInventoryItems",
                column: "AppId",
                principalTable: "SteamApps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamProfileInventoryItems_SteamCurrencies_CurrencyId",
                table: "SteamProfileInventoryItems",
                column: "CurrencyId",
                principalTable: "SteamCurrencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamProfileInventoryItems_SteamAssetDescriptions_DescriptionId",
                table: "SteamProfileInventoryItems",
                column: "DescriptionId",
                principalTable: "SteamAssetDescriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamProfileInventoryItems_SteamProfiles_ProfileId",
                table: "SteamProfileInventoryItems",
                column: "ProfileId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamProfileInventoryItems_SteamApps_AppId",
                table: "SteamProfileInventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamProfileInventoryItems_SteamCurrencies_CurrencyId",
                table: "SteamProfileInventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamProfileInventoryItems_SteamAssetDescriptions_DescriptionId",
                table: "SteamProfileInventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamProfileInventoryItems_SteamProfiles_ProfileId",
                table: "SteamProfileInventoryItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SteamProfileInventoryItems",
                table: "SteamProfileInventoryItems");

            migrationBuilder.RenameTable(
                name: "SteamProfileInventoryItems",
                newName: "SteamInventoryItems");

            migrationBuilder.RenameIndex(
                name: "IX_SteamProfileInventoryItems_ProfileId",
                table: "SteamInventoryItems",
                newName: "IX_SteamInventoryItems_ProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_SteamProfileInventoryItems_DescriptionId",
                table: "SteamInventoryItems",
                newName: "IX_SteamInventoryItems_DescriptionId");

            migrationBuilder.RenameIndex(
                name: "IX_SteamProfileInventoryItems_CurrencyId",
                table: "SteamInventoryItems",
                newName: "IX_SteamInventoryItems_CurrencyId");

            migrationBuilder.RenameIndex(
                name: "IX_SteamProfileInventoryItems_AppId",
                table: "SteamInventoryItems",
                newName: "IX_SteamInventoryItems_AppId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SteamInventoryItems",
                table: "SteamInventoryItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamInventoryItems_SteamApps_AppId",
                table: "SteamInventoryItems",
                column: "AppId",
                principalTable: "SteamApps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamInventoryItems_SteamCurrencies_CurrencyId",
                table: "SteamInventoryItems",
                column: "CurrencyId",
                principalTable: "SteamCurrencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamInventoryItems_SteamAssetDescriptions_DescriptionId",
                table: "SteamInventoryItems",
                column: "DescriptionId",
                principalTable: "SteamAssetDescriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamInventoryItems_SteamProfiles_ProfileId",
                table: "SteamInventoryItems",
                column: "ProfileId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
