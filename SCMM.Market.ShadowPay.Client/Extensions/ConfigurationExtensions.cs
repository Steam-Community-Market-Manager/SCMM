using Microsoft.Extensions.Configuration;

namespace SCMM.Market.ShadowPay.Client.Extensions
{
    public static class ConfigurationExtensions
    {
        public static ShadowPayConfiguration GetShadowPayConfiguration(this IConfiguration configuration)
        {
            return configuration
                .GetSection("Market")
                .GetSection("ShadowPay")
                .Get<ShadowPayConfiguration>();
        }
    }
}
