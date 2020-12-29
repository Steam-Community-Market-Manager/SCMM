using System;

namespace SCMM.Web.Shared.Data.Models.Steam
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
