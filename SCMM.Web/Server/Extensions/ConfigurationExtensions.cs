using Microsoft.Extensions.Configuration;
using SCMM.Discord.Client;
using SCMM.Steam.Client;

namespace SCMM.Web.Server.Extensions
{
    public static class ConfigurationExtensions
    {
        public static DiscordConfiguration GetDiscoardConfiguration(this IConfiguration configuration)
        {
            return configuration
                .GetSection("Discord")
                .Get<DiscordConfiguration>();
        }

        public static SteamConfiguration GetSteamConfiguration(this IConfiguration configuration)
        {
            return configuration
                .GetSection("Steam")
                .Get<SteamConfiguration>();
        }

        public static string GetBaseUrl(this IConfiguration configuration)
        {
            return configuration.GetValue<string>("BaseUrl");
        }
    }
}
