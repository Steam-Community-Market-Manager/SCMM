using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCMM.Discord.Client
{
    public class DiscordUser
    {
        private SocketUser _user;

        public DiscordUser(SocketUser user)
        {
            _user = user;
        }

        public string Username => _user?.Username;

        public string Discriminator => _user?.Discriminator;

        public string AvatarUrl => _user?.GetAvatarUrl();

        public string Status => _user?.Status.ToString();

        public string Activity => $"{_user?.Activity?.Type.ToString()} {_user?.Activity?.Name}";
    }
}
