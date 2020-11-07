using System;

namespace SCMM.Web.Shared.Data.Models.Steam
{
    [Flags]
    public enum SteamProfileInventoryItemFlags : byte
    {
        None = 0x00,
        Tradable = 0x01,
        Marketable = 0x02,

        WantToTrade = 0x10,
        WantToSell = 0x20,

        Pinned = 0x80
    }
}
