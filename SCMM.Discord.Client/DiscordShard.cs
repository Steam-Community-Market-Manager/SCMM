using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCMM.Discord.Client
{
    public class DiscordShard
    {
        private DiscordSocketClient _client;

        public DiscordShard(DiscordSocketClient client)
        {
            _client = client;
        }

        public int Id => _client?.ShardId ?? 0;

        public string ConnectionState => _client?.ConnectionState.ToString();

        public string LoginState => _client?.LoginState.ToString();

        public int Latency => _client?.Latency ?? 0;

        public int Guilds => _client?.Guilds.Count ?? 0;
    }
}
