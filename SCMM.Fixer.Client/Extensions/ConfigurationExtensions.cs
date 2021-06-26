using Microsoft.Extensions.Configuration;

namespace SCMM.Fixer.Client.Extensions
{
    public static class ConfigurationExtensions
    {
        public static FixerConfiguration GetFixerConfiguration(this IConfiguration configuration)
        {
            return configuration
                .GetSection("Fixer")
                .Get<FixerConfiguration>();
        }
    }
}
