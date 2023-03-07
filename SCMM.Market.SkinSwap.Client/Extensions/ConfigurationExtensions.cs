using Microsoft.Extensions.Configuration;

namespace SCMM.Market.SkinSwap.Client.Extensions
{
    public static class ConfigurationExtensions
    {
        public static SkinSwapConfiguration GetSkinSwapConfiguration(this IConfiguration configuration)
        {
            return configuration
                .GetSection("Market")
                .GetSection("SkinSwap")
                .Get<SkinSwapConfiguration>();
        }
    }
}
