using System.Net;

namespace SCMM.Shared.Web.Client;

public class WebProxyAwareHttpHandler : DelegatingHandler
{
    private readonly IWebProxyManager _webProxyManager;
    private readonly Func<TimeSpan?> _webProxyCooldownResolver;

    public WebProxyAwareHttpHandler(IWebProxyManager webProxyManager, Func<TimeSpan?> webProxyCooldownResolver, HttpMessageHandler innerHandler = null)
    {
        _webProxyManager = webProxyManager;
        _webProxyCooldownResolver = webProxyCooldownResolver;
        InnerHandler = innerHandler ?? new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
    {
        var requestUri = request?.RequestUri;
        var responseCode = (HttpStatusCode?) null;

        try
        {
            // Send the http request
            var response = await base.SendAsync(request, cancellationToken);
            responseCode = response?.StatusCode;
            return response;
        }
        finally
        {
            // Update the web proxy usage statistics based on the http response code
            if (_webProxyManager != null && requestUri != null)
            {
                var proxyId = _webProxyManager.GetProxyId(requestUri);
                if (!string.IsNullOrEmpty(proxyId))
                {
                    _webProxyManager.UpdateProxyRequestStatistics(proxyId, requestUri, responseCode);
                    switch (responseCode)
                    {
                        case HttpStatusCode.TooManyRequests:
                            var cooldown = _webProxyCooldownResolver.Invoke();
                            if (cooldown != null)
                            {
                                _webProxyManager.CooldownProxy(proxyId, requestUri, cooldown.Value);
                            }
                            break;

                        case HttpStatusCode.ProxyAuthenticationRequired:
                            _webProxyManager.DisableProxy(proxyId);
                            break;
                    }
                }
            }
        }
    }
}
