using Microsoft.Extensions.Configuration;
using SCMM.Steam.Shared;

namespace SCMM.Web.Server.Domain
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
