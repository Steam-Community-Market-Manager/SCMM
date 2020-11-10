﻿using System;

namespace SCMM.Web.Shared.Data.Models.Steam
{
    [Flags]
    public enum SteamProfileMarketItemFlags : byte
    {
        None = 0x00,
        Watching = 0x01,
        WantToBuy = 0x02
    }
}
