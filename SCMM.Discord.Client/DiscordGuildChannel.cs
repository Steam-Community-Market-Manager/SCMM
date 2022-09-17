using Discord.WebSocket;

namespace SCMM.Discord.Client
{
    public class DiscordGuildChannel
    {
        private readonly SocketGuildChannel _guildChannel;

        public DiscordGuildChannel(SocketGuildChannel guildChannel)
        {
            _guildChannel = guildChannel;
        }

        public ulong Id => _guildChannel?.Id ?? 0;

        public string Name => _guildChannel?.Name;
    }
}
