using Microsoft.Extensions.Configuration;
using SCMM.Webshare.Proxy.Client;

namespace SCMM.Fixer.Client.Extensions
{
    public static class ConfigurationExtensions
    {
        public static WebshareConfiguration GetWebshareConfiguration(this IConfiguration configuration)
        {
            return configuration
                .GetSection("Webshare")
                .Get<WebshareConfiguration>();
        }
    }
}
