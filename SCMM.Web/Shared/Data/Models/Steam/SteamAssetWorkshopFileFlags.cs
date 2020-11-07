using System;

namespace SCMM.Web.Shared.Data.Models.Steam
{
    [Flags]
    public enum SteamAssetWorkshopFileFlags : byte
    {
        None = 0x00,
        Banned = 0x01
    }
}
