using Microsoft.Extensions.Configuration;

namespace SCMM.Market.Buff.Client.Extensions
{
    public static class ConfigurationExtensions
    {
        public static BuffConfiguration GetBuffConfiguration(this IConfiguration configuration)
        {
            return configuration
                .GetSection("Market")
                .GetSection("Buff")
                .Get<BuffConfiguration>();
        }
    }
}
