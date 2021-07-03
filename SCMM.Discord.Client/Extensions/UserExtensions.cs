using Discord;
using System;
using System.Threading.Tasks;

namespace SCMM.Discord.Client.Extensions
{
    public static class UserExtensions
    {
        public static string GetFullUsername(this IUser user)
        {
            return $"{user.Username}#{user.Discriminator}";
        }
    }
}
