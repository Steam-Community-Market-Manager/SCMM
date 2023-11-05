using Microsoft.Extensions.Logging;
using System.Net;

namespace SCMM.Shared.Web.Client;

public class WebProxyAwareHttpMessageHandler : DelegatingHandler
{
    private readonly ILogger _logger;
    private readonly IWebProxyManager _webProxyManager;
    private readonly Func<TimeSpan?> _webProxyCooldownTimeResolver;

    public WebProxyAwareHttpMessageHandler(ILogger logger, IWebProxyManager webProxyManager, Func<TimeSpan?> webProxyCooldownTimeResolver, HttpMessageHandler innerHandler = null)
    {
        _logger = logger;
        _webProxyManager = webProxyManager;
        _webProxyCooldownTimeResolver = webProxyCooldownTimeResolver;
        InnerHandler = innerHandler ?? new HttpClientHandler();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
    {
        var requestUri = request?.RequestUri;
        var responseCode = (HttpStatusCode?) null;

        try
        {
            // Send the http request
            // NOTE: We must execute the task synchronously to ensure that we can interact with the web proxy instance that handled the request afterwards.
            //       Web proxies are assigned per-thread, so this thread needs to execute both the SendAsync() call and finally block below.
            // SEE:  SCMM.Shared.Web.Client.RotatingWebProxy::GetProxy()
            var sendTask = base.SendAsync(request, cancellationToken);
            sendTask.Wait();

            // Parse the htp response
            var response = sendTask.Result;
            responseCode = response?.StatusCode;
            return sendTask;
        }
        finally
        {
            // Update the web proxy state based on the received http response code
            var proxyId = _webProxyManager.CurrentProxyId; // This only works in the same thread that called SendAsync()
            if (!string.IsNullOrEmpty(proxyId) && requestUri != null)
            {
                // Update proxy usage statistics
                _webProxyManager.UpdateProxyRequestStatistics(proxyId, requestUri, responseCode);
                switch (responseCode)
                {
                    // Response was rate-limited, increase the proxies cooldown for the requested domain
                    case HttpStatusCode.TooManyRequests:
                        var cooldownTime = _webProxyCooldownTimeResolver.Invoke();
                        if (cooldownTime != null)
                        {
                            _webProxyManager.CooldownProxy(proxyId, requestUri, cooldownTime.Value);
                        }
                        break;

                    // Proxy auth is stale, disable the proxy
                    case HttpStatusCode.ProxyAuthenticationRequired:
                        _webProxyManager.DisableProxy(proxyId);
                        break;
                }
            }
            else
            {
                _logger.LogError(
                    $"Unable to update web proxy statistics for request '{requestUri}' with response '{responseCode}', no proxy id is set on the current thread. " +
                    $"Either the request didn't pass through a web proxy or the thread that handled sending the request differs from the thread that handled the response."
                );
            }
        }
    }
}
