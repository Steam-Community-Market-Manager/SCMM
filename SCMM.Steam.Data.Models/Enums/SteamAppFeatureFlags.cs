namespace SCMM.Steam.Data.Models.Enums
{
    [Flags]
    public enum SteamAppFeatureFlags : long
    {
        None = 0x0,

        ItemDefinitions = 0x1,

        ItemWorkshop = 0x100,
        ItemWorkshopSubmissionTracking = 0x200,
        ItemWorkshopAcceptedTracking = 0x400,

        ItemStore = 0x10000,
        ItemStoreBrowser = 0x20000,
        ItemStorePriceTracking = 0x40000,
        ItemStoreMediaTracking = 0x80000,
        ItemStorePersistent = 0x100000,
        ItemStoreRotating = 0x200000,

        ItemMarket = 0x1000000,
        ItemMarketPriceTracking = 0x2000000,
        ItemMarketActivityTracking = 0x4000000,
        ItemMarketNotifications = 0x8000000,

        ItemInventory = 0x100000000,
        ItemInventoryTracking = 0x200000000,

        AssetDescriptionTracking = 0x1000000000000,
        AssetDescriptionIconCaching = 0x2000000000000,

        AssetDescriptionFeatureCrafting = 0x10000000000000,
        AssetDescriptionFeatureGlowing = 0x20000000000000,
        AssetDescriptionFeatureCutout = 0x40000000000000,
        AssetDescriptionFeaturePublisherDrops = 0x80000000000000,
        AssetDescriptionFeatureTwitchDrops = 0x100000000000000,
        AssetDescriptionFeatureLootCrates = 0x200000000000000
    }
}
