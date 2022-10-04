using Microsoft.Extensions.Configuration;

namespace SCMM.Worker.Client.Configuration;

public static class ConfigurationExtensions
{
    public static IEnumerable<WebProxyEndpoint> GetWebProxyConfiguration(this IConfiguration configuration)
    {
        return configuration
            .GetSection("WebProxies")
            .Get<WebProxyEndpoint[]>();
    }
}