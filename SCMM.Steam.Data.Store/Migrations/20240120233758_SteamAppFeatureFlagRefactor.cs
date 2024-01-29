using Microsoft.EntityFrameworkCore.Migrations;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    /// <inheritdoc />
    public partial class SteamAppFeatureFlagRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Features",
                table: "SteamApps");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "SteamApps");

            migrationBuilder.RenameColumn(
                name: "TradableAndMarketablAfter",
                table: "SteamProfileInventoryItems",
                newName: "TradableAndMarketableAfter");

            migrationBuilder.AlterColumn<long>(
                name: "Flags",
                table: "SteamProfileMarketItems",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<long>(
                name: "Flags",
                table: "SteamProfileInventoryItems",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AddColumn<decimal>(
                name: "FeatureFlags",
                table: "SteamApps",
                type: "decimal(20,0)",
                nullable: false,
                defaultValue: 0m);

            var csgoFeatureFlags = (
                SteamAppFeatureFlags.ItemStore |
                SteamAppFeatureFlags.ItemStorePriceTracking |
                SteamAppFeatureFlags.ItemMarket |
                SteamAppFeatureFlags.ItemInventory
            );
            migrationBuilder.Sql($"UPDATE [SteamApps] SET [FeatureFlags] = {(long)csgoFeatureFlags} WHERE [SteamId] = '{Constants.CSGOAppId}'");

            var rustFeatureFlags = (
                SteamAppFeatureFlags.ItemDefinitions |
                SteamAppFeatureFlags.ItemWorkshop |
                SteamAppFeatureFlags.ItemWorkshopSubmissionTracking |
                SteamAppFeatureFlags.ItemWorkshopAcceptedTracking |
                SteamAppFeatureFlags.ItemStore |
                SteamAppFeatureFlags.ItemStoreWebBrowser |
                //SteamAppFeatureFlags.ItemStorePriceTracking | // Rust gets this from item definition updates instead
                SteamAppFeatureFlags.ItemStoreMediaTracking |
                SteamAppFeatureFlags.ItemStorePersistent |
                SteamAppFeatureFlags.ItemStoreRotating |
                SteamAppFeatureFlags.ItemMarket |
                SteamAppFeatureFlags.ItemMarketPriceTracking |
                SteamAppFeatureFlags.ItemMarketActivityTracking |
                SteamAppFeatureFlags.ItemMarketNotifications |
                SteamAppFeatureFlags.ItemInventory |
                SteamAppFeatureFlags.ItemInventoryTracking |
                SteamAppFeatureFlags.AssetDescriptionTracking |
                SteamAppFeatureFlags.AssetDescriptionSupplyTracking |
                SteamAppFeatureFlags.AssetDescriptionIconCaching |
                SteamAppFeatureFlags.AssetDescriptionFeatureCrafting |
                SteamAppFeatureFlags.AssetDescriptionFeatureGlowing |
                SteamAppFeatureFlags.AssetDescriptionFeatureCutout |
                SteamAppFeatureFlags.AssetDescriptionFeaturePublisherDrops |
                SteamAppFeatureFlags.AssetDescriptionFeatureTwitchDrops |
                SteamAppFeatureFlags.AssetDescriptionFeatureLootCrates
            );
            migrationBuilder.Sql($"UPDATE [SteamApps] SET [FeatureFlags] = {(long)rustFeatureFlags} WHERE [SteamId] = '{Constants.RustAppId}'");

            var unturnedFeatureFlags = (
                SteamAppFeatureFlags.ItemStore |
                SteamAppFeatureFlags.ItemStoreWebBrowser |
                SteamAppFeatureFlags.ItemStorePriceTracking |
                SteamAppFeatureFlags.ItemMarket |
                SteamAppFeatureFlags.ItemInventory
            );
            migrationBuilder.Sql($"UPDATE [SteamApps] SET [FeatureFlags] = {(long)unturnedFeatureFlags} WHERE [SteamId] = '{Constants.UnturnedAppId}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeatureFlags",
                table: "SteamApps");

            migrationBuilder.RenameColumn(
                name: "TradableAndMarketableAfter",
                table: "SteamProfileInventoryItems",
                newName: "TradableAndMarketablAfter");

            migrationBuilder.AlterColumn<byte>(
                name: "Flags",
                table: "SteamProfileMarketItems",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<byte>(
                name: "Flags",
                table: "SteamProfileInventoryItems",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "Features",
                table: "SteamApps",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "SteamApps",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
