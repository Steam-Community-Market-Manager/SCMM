using Microsoft.Extensions.Configuration;

namespace SCMM.Market.DMarket.Client.Extensions
{
    public static class ConfigurationExtensions
    {
        public static DMarketConfiguration GetDMarketConfiguration(this IConfiguration configuration)
        {
            return configuration
                .GetSection("Market")
                .GetSection("DMarket")
                .Get<DMarketConfiguration>();
        }
    }
}
