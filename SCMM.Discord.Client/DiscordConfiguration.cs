namespace SCMM.Discord.Client
{
    public class DiscordConfiguration
    {
        public string BotToken { get; set; }

        public string CommandPrefix { get; set; }

        public int? ShardId { get; set; }

        public int TotalShards { get; set; }

        public string InviteUrl { get; set; }

        public ulong AppId { get; set; }
    }
}
