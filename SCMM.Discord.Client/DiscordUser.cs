﻿using Discord.WebSocket;

namespace SCMM.Discord.Client
{
    public class DiscordUser
    {
        private readonly SocketUser _user;

        public DiscordUser(SocketUser user)
        {
            _user = user;
        }

        public string Username => _user?.Username;

        public string Discriminator => _user?.Discriminator;

        public string AvatarUrl => _user?.GetAvatarUrl();

        public string Status => _user?.Status.ToString();

        public string Activity => String.Join(", ", _user?.Activities.Select(x => $"{x.Type} {x.Name}"));
    }
}
