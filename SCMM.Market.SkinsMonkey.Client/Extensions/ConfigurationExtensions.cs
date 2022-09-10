using Microsoft.Extensions.Configuration;

namespace SCMM.Market.SkinsMonkey.Client.Extensions
{
    public static class ConfigurationExtensions
    {
        public static SkinsMonkeyConfiguration GetSkinsMonkeyConfiguration(this IConfiguration configuration)
        {
            return configuration
                .GetSection("Market")
                .GetSection("SkinsMonkey")
                .Get<SkinsMonkeyConfiguration>();
        }
    }
}
