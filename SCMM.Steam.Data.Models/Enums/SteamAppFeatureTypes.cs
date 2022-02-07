namespace SCMM.Steam.Data.Models.Enums
{
    [Flags]
    public enum SteamAppFeatureTypes : uint
    {
        None = 0x0000,

        Store = (StorePersistent | StoreRotating),
        StorePersistent = 0x0001,
        StoreRotating = 0x0002,
        
        ItemWorkshop = 0x0010,
        ItemCrafting = 0x0020
    }
}
