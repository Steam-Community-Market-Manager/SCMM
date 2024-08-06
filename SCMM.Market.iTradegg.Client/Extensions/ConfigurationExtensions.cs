using Microsoft.Extensions.Configuration;

namespace SCMM.Market.iTradegg.Client.Extensions
{
    public static class ConfigurationExtensions
    {
        public static iTradeggConfiguration GetiTradeggConfiguration(this IConfiguration configuration)
        {
            return configuration
                .GetSection("Market")
                .GetSection("iTradegg")
                .Get<iTradeggConfiguration>();
        }
    }
}
