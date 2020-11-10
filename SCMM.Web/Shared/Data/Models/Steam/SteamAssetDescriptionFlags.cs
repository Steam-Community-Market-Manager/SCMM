using System;

namespace SCMM.Web.Shared.Data.Models.Steam
{
    [Flags]
    public enum SteamAssetDescriptionFlags : byte
    {
        None = 0x00,
        Tradable = 0x01,
        Marketable = 0x02,
        Commodity = 0x10
    }
}
