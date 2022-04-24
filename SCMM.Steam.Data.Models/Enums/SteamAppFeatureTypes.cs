namespace SCMM.Steam.Data.Models.Enums
{
    [Flags]
    public enum SteamAppFeatureTypes : uint
    {
        None = 0x0000,

        StorePersistent = 0x0001,
        StoreRotating = 0x0002,
        
        ItemWorkshop = 0x0010,

        ItemFeatureCrafting = 0x0100,
        ItemFeatureGlowing = 0x0200,
        ItemFeatureCutout = 0x0400,
        ItemFeatureGameDrops = 0x0800,
        ItemFeatureTwitchDrops = 0x1000
    }
}
