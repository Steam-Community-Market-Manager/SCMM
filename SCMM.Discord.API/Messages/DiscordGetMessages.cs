using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Discord.API.Messages
{
    [Queue(Name = "Discord-Get-Messages")]
    public class DiscordGetMessages : IMessage
    {
        public ulong GuildId { get; set; }

        public ulong ChannelId { get; set; }

        public int MessageLimit { get; set; } = 10;
    }
}
