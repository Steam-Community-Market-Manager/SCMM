﻿using System;

namespace SCMM.Web.Data.Models.Steam
{
    [Flags]
    public enum SteamProfileFlags : byte
    {
        None = 0x00,
        TradeBanned = 0x01
    }
}