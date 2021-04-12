using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddUniqueIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SteamItemStores_AppId",
                table: "SteamItemStores");

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamStoreItems",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamProfiles",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DiscordId",
                table: "SteamProfiles",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamProfileMarketItems",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamProfileInventoryItems",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamMarketItems",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamLanguages",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamCurrencies",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamAssetWorkshopFiles",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamAssetDescriptions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamApps",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DiscordId",
                table: "DiscordGuilds",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_SteamStoreItems_SteamId_DescriptionId",
                table: "SteamStoreItems",
                columns: new[] { "SteamId", "DescriptionId" },
                unique: true,
                filter: "[SteamId] IS NOT NULL AND [DescriptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfiles_DiscordId",
                table: "SteamProfiles",
                column: "DiscordId",
                unique: true,
                filter: "[DiscordId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfiles_SteamId",
                table: "SteamProfiles",
                column: "SteamId",
                unique: true,
                filter: "[SteamId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfileMarketItems_SteamId_DescriptionId_ProfileId",
                table: "SteamProfileMarketItems",
                columns: new[] { "SteamId", "DescriptionId", "ProfileId" },
                unique: true,
                filter: "[SteamId] IS NOT NULL AND [DescriptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfileInventoryItems_SteamId_DescriptionId_ProfileId",
                table: "SteamProfileInventoryItems",
                columns: new[] { "SteamId", "DescriptionId", "ProfileId" },
                unique: true,
                filter: "[SteamId] IS NOT NULL AND [DescriptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItems_SteamId_DescriptionId",
                table: "SteamMarketItems",
                columns: new[] { "SteamId", "DescriptionId" },
                unique: true,
                filter: "[SteamId] IS NOT NULL AND [DescriptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SteamLanguages_SteamId",
                table: "SteamLanguages",
                column: "SteamId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamItemStores_AppId_Start_End",
                table: "SteamItemStores",
                columns: new[] { "AppId", "Start", "End" },
                unique: true,
                filter: "[End] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SteamCurrencies_SteamId",
                table: "SteamCurrencies",
                column: "SteamId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetWorkshopFiles_SteamId",
                table: "SteamAssetWorkshopFiles",
                column: "SteamId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_SteamId",
                table: "SteamAssetDescriptions",
                column: "SteamId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamApps_SteamId",
                table: "SteamApps",
                column: "SteamId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscordGuilds_DiscordId",
                table: "DiscordGuilds",
                column: "DiscordId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SteamStoreItems_SteamId_DescriptionId",
                table: "SteamStoreItems");

            migrationBuilder.DropIndex(
                name: "IX_SteamProfiles_DiscordId",
                table: "SteamProfiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamProfiles_SteamId",
                table: "SteamProfiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamProfileMarketItems_SteamId_DescriptionId_ProfileId",
                table: "SteamProfileMarketItems");

            migrationBuilder.DropIndex(
                name: "IX_SteamProfileInventoryItems_SteamId_DescriptionId_ProfileId",
                table: "SteamProfileInventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_SteamMarketItems_SteamId_DescriptionId",
                table: "SteamMarketItems");

            migrationBuilder.DropIndex(
                name: "IX_SteamLanguages_SteamId",
                table: "SteamLanguages");

            migrationBuilder.DropIndex(
                name: "IX_SteamItemStores_AppId_Start_End",
                table: "SteamItemStores");

            migrationBuilder.DropIndex(
                name: "IX_SteamCurrencies_SteamId",
                table: "SteamCurrencies");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetWorkshopFiles_SteamId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_SteamId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropIndex(
                name: "IX_SteamApps_SteamId",
                table: "SteamApps");

            migrationBuilder.DropIndex(
                name: "IX_DiscordGuilds_DiscordId",
                table: "DiscordGuilds");

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamStoreItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DiscordId",
                table: "SteamProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamProfileMarketItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamProfileInventoryItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamMarketItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamLanguages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamCurrencies",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamAssetWorkshopFiles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamApps",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "DiscordId",
                table: "DiscordGuilds",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_SteamItemStores_AppId",
                table: "SteamItemStores",
                column: "AppId");
        }
    }
}
