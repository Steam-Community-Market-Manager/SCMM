using System;

namespace SCMM.Steam.Data.Models.Enums
{
    [Flags]
    public enum DiscordGuildFlags : byte
    {
        None = 0x00,
        VIP = 0x01
    }
}
