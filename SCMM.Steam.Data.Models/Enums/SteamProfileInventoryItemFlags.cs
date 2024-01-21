namespace SCMM.Steam.Data.Models.Enums
{
    [Flags]
    public enum SteamProfileInventoryItemFlags : uint
    {
        None = 0x00,
        Investment = 0x01,
        WantToTrade = 0x10,
        WantToSell = 0x20
    }
}
