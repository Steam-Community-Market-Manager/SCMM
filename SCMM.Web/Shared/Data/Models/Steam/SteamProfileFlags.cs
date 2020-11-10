using System;

namespace SCMM.Web.Shared.Data.Models.Steam
{
    [Flags]
    public enum SteamProfileFlags : byte
    {
        None = 0x00,
        TradeBanned = 0x01
    }
}
