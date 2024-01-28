namespace SCMM.Discord.Data.Store
{
    [Flags]
    public enum DiscordGuildFeatureFlags : byte
    {
        None = 0x00,
        VIP = 0x01,
        Alerts = 0x02
    }
}
