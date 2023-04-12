namespace SCMM.Steam.Data.Models.Enums
{
    [Flags]
    public enum SteamAppFeatureTypes : uint
    {
        None = 0x0000,

        ItemStorePersistent = 0x0001,
        ItemStoreRotating = 0x0002,

        ItemWorkshop = 0x0010,
        ItemDefinitionArchives = 0x0020,

        ItemFeatureCrafting = 0x0100,
        ItemFeatureGlowing = 0x0200,
        ItemFeatureCutout = 0x0400,
        ItemFeaturePublisherDrops = 0x0800,
        ItemFeatureTwitchDrops = 0x1000,
        ItemFeatureLootCrates = 0x2000
    }
}
