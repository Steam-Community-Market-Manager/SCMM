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

    public static IEnumerable<WebProxyEndpoint> Expand(this IEnumerable<WebProxyEndpoint> webProxyEndpoints)
    {
        var expandedEndpoints = webProxyEndpoints?.ToList();
        if (webProxyEndpoints != null)
        {
            var webProxyListEndpoints = webProxyEndpoints
                .Where(x => new Uri(x.Url).Scheme == Uri.UriSchemeHttps)
                .Where(x => x.IsEnabled)
                .ToArray();

            foreach (var webProxyListEndpoint in webProxyListEndpoints)
            {
                var index = expandedEndpoints.IndexOf(webProxyListEndpoint);
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        var listResponse = httpClient.Send(new HttpRequestMessage(HttpMethod.Get, webProxyListEndpoint.Url));
                        listResponse.EnsureSuccessStatusCode();

                        using (var listReader = new StreamReader(listResponse.Content.ReadAsStream()))
                        {
                            while (!listReader.EndOfStream)
                            {
                                var webProxyEndpoint = listReader.ReadLine().ToWebProxyEndpoint();
                                if (webProxyEndpoint != null)
                                {
                                    expandedEndpoints.Insert(index, webProxyEndpoint);
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }
                finally
                {
                    expandedEndpoints.Remove(webProxyListEndpoint);
                }
            }
        }

        return expandedEndpoints;
    }

    private static WebProxyEndpoint ToWebProxyEndpoint(this string value)
    {
        var values = value?.Split(':', StringSplitOptions.TrimEntries);
        if (values?.Length >= 2)
        {
            return new WebProxyEndpoint()
            {
                Url = new UriBuilder(Uri.UriSchemeHttp, values.ElementAtOrDefault(0), int.Parse(values.ElementAtOrDefault(1) ?? "3128")).Uri.ToString(),
                Username = values.ElementAtOrDefault(2),
                Password = values.ElementAtOrDefault(3),
                IsEnabled = !String.IsNullOrEmpty(values.ElementAtOrDefault(0))
            };
        }

        return null;
    }
}