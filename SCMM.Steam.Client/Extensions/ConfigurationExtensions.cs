using Microsoft.Extensions.Configuration;

namespace SCMM.Steam.Client.Extensions
{
    public static class ConfigurationExtensions
    {
        public static SteamConfiguration GetSteamConfiguration(this IConfiguration configuration)
        {
            return configuration
                .GetSection("Steam")
                .Get<SteamConfiguration>();
        }
    }
}
