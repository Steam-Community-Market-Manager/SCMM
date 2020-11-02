using System;

namespace SCMM.Web.Server.Data.Models.Steam
{
    [Flags]
    public enum SteamProfileMarketItemFlags : byte
    {
        None = 0x00,
        Watching = 0x01,
        WantToBuy = 0x02
    }
}
