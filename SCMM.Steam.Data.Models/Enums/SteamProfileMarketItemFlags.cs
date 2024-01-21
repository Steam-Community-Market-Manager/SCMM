namespace SCMM.Steam.Data.Models.Enums
{
    [Flags]
    public enum SteamProfileMarketItemFlags : uint
    {
        None = 0x00,
        Watching = 0x01,
        WantToBuy = 0x10
    }
}
