﻿namespace SCMM.Steam.Data.Models.Enums
{
    [Flags]
    public enum SteamProfileInventoryItemFlags : byte
    {
        None = 0x00,
        Investment = 0x01,
        WantToTrade = 0x10,
        WantToSell = 0x20
    }
}
