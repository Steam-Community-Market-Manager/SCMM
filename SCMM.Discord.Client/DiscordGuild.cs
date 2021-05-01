using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCMM.Discord.Client
{
    public class DiscordGuild
    {
        private SocketGuild _guild;

        public DiscordGuild(SocketGuild guild)
        {
            _guild = guild;
        }

        public ulong Id => _guild?.Id ?? 0;

        public string Name => _guild?.Name;

        public string IconUrl => _guild?.IconUrl;
    }
}
