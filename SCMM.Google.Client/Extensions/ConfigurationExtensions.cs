using Microsoft.Extensions.Configuration;

namespace SCMM.Google.Client.Extensions
{
    public static class ConfigurationExtensions
    {
        public static GoogleConfiguration GetGoogleConfiguration(this IConfiguration configuration)
        {
            return configuration
                .GetSection("Google")
                .Get<GoogleConfiguration>();
        }
    }
}
