using Microsoft.Extensions.Configuration;

namespace SCMM.Azure.AI.Extensions
{
    public static class ConfigurationExtensions
    {
        public static AzureAiConfiguration GetAzureAiConfiguration(this IConfiguration configuration)
        {
            return configuration
                .GetSection("AzureAi")
                .Get<AzureAiConfiguration>();
        }
    }
}
