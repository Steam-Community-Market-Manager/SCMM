using System;

namespace SCMM.Steam.Data.Models.Enums
{
    [Flags]
    public enum SteamProfileInventoryItemFlags : byte
    {
        None = 0x00,
        Tradable = 0x02,
        Marketable = 0x04,
        WantToTrade = 0x10,
        WantToSell = 0x20
    }
}
