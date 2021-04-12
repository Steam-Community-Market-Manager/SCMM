using System;

namespace SCMM.Steam.Data.Models.Enums
{
    [Flags]
    public enum SteamAssetWorkshopFileFlags : byte
    {
        None = 0x00,
        Banned = 0x01
    }
}
