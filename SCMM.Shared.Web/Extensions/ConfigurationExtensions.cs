using Microsoft.Extensions.Configuration;

namespace SCMM.Shared.Web.Extensions
{
    public static class ConfigurationExtensions
    {
        public static string GetWebsiteUrl(this IConfiguration configuration)
        {
            return configuration.GetValue<string>("WebsiteUrl");
        }
    }
}
