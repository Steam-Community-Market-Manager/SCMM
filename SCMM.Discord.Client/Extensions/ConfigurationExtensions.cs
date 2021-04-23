using Microsoft.Extensions.Configuration;

namespace SCMM.Discord.Client.Extensions
{
    public static class ConfigurationExtensions
    {
        public static DiscordConfiguration GetDiscordConfiguration(this IConfiguration configuration)
        {
            return configuration
                .GetSection("Discord")
                .Get<DiscordConfiguration>();
        }
    }
}
