using Microsoft.Extensions.Configuration;

namespace SCMM.Shared.Client.Configuration;

public static class ConfigurationExtensions
{
    public static IEnumerable<WebProxyEndpoint> GetWebProxyConfiguration(this IConfiguration configuration)
    {
        return configuration
            .GetSection("WebProxies")
            .Get<WebProxyEndpoint[]>();
    }
}