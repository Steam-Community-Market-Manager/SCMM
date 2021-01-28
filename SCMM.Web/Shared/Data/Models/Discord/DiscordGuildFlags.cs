using System;

namespace SCMM.Web.Shared.Data.Models.Discord
{
    [Flags]
    public enum DiscordGuildFlags : byte
    {
        None = 0x00,
        VIP = 0x01
    }
}
