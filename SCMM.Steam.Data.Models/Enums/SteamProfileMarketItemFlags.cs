﻿namespace SCMM.Steam.Data.Models.Enums
{
    [Flags]
    public enum SteamProfileMarketItemFlags : int
    {
        None = 0x00,
        Watching = 0x01,
        WantToBuy = 0x10
    }
}
