using System.Net;
using System.Net.Http.Headers;

namespace SCMM.Shared.Client;

public class WebClient : IDisposable
{
    private readonly IDictionary<string, string> _defaultHeaders;
    private readonly CookieContainer _cookieContainer;
    private readonly IWebProxy _webProxy;
    private readonly HttpMessageHandler _httpHandler;
    private bool _disposedValue;

    public WebClient(CookieContainer cookieContainer = null, IWebProxy webProxy = null)
    {
        _defaultHeaders = new Dictionary<string, string>();
        _cookieContainer = cookieContainer;
        _webProxy = webProxy;
        _httpHandler = new HttpClientHandler()
        {
            UseCookies = cookieContainer != null,
            CookieContainer = cookieContainer ?? new CookieContainer(),
            Proxy = webProxy,
            PreAuthenticate = (webProxy != null),
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 3,
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (_webProxy == null ? null :
                // Http web proxy might MiTM the SSL certificate, so ignore invalid certs when using a proxy
                (httpRequestMessage, cert, cetChain, policyErrors) => true
            )
        };
    }

    protected HttpClient BuildHttpClient()
    {
        var httpClient = new HttpClient(_httpHandler, false);

        // Set default client headers
        if (_defaultHeaders != null)
        {
            foreach (var header in _defaultHeaders)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        // If these headers weren't set by the client, use these sensible defaults
        if (!httpClient.DefaultRequestHeaders.Accept.Any())
        {
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        }
        if (!httpClient.DefaultRequestHeaders.AcceptEncoding.Any())
        {
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        }
        if (!httpClient.DefaultRequestHeaders.AcceptLanguage.Any())
        {
            httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
            httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en", 0.9));
        }
        if (httpClient.DefaultRequestHeaders.IfModifiedSince == null && IfModifiedSinceTimeAgo != null)
        {
            httpClient.DefaultRequestHeaders.IfModifiedSince = DateTimeOffset.UtcNow.Subtract(IfModifiedSinceTimeAgo.Value);
        }

        return httpClient;
    }

    protected HttpClient BuildWebApiHttpClient(Uri host = null, string authHeaderName = null, string authHeaderFormat = null, string authKey = null)
    {
        var httpClient = BuildHttpClient();

        if (host != null)
        {
            httpClient.DefaultRequestHeaders.Host = host.Host;
        }

        if (!string.IsNullOrEmpty(authHeaderName) && !string.IsNullOrEmpty(authHeaderFormat) && !string.IsNullOrEmpty(authKey))
        {
            if (!httpClient.DefaultRequestHeaders.Contains(authHeaderName))
            {
                httpClient.DefaultRequestHeaders.Add(authHeaderName, String.Format(authHeaderFormat, authKey));
            }
        }

        return httpClient;
    }

    protected HttpClient BuildWebBrowserHttpClient(Uri referrer = null)
    {
        var httpClient = BuildHttpClient();

        // We are a normal looking web browser, honest (helps with WAF rules that block bots)
        if (!httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(Windows NT 10.0; Win64; x64)"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AppleWebKit", "537.36"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(KHTML, like Gecko)"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Chrome", "96.0.4664.110"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Safari", "537.36"));
        }

        // We made this request from a web browser, honest (helps with WAF rules that enforce OWASP)
        if (!httpClient.DefaultRequestHeaders.Contains("X-Requested-With"))
        {
            httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        }

        if (referrer != null)
        {
            // We made this request from your website, honest...
            httpClient.DefaultRequestHeaders.Host = referrer.Host;
            httpClient.DefaultRequestHeaders.Referrer = referrer;
        }

        return httpClient;
    }

    protected async Task<HttpResponseMessage> PostAsync<TRequest>(HttpClient client, TRequest request, HttpContent content = null, CancellationToken cancellationToken = default(CancellationToken))
        where TRequest : IWebRequest
    {
        return HandleRequestAndAssertWasSuccess(request,
            await client.PostAsync(request.Uri, content, cancellationToken)
        );
    }

    protected async Task<HttpResponseMessage> GetAsync<TRequest>(HttpClient client, TRequest request, CancellationToken cancellationToken = default(CancellationToken))
        where TRequest : IWebRequest
    {
        return HandleRequestAndAssertWasSuccess(request,
            await client.GetAsync(request.Uri, cancellationToken)
        );
    }

    protected HttpResponseMessage HandleRequestAndAssertWasSuccess(IWebRequest request, HttpResponseMessage response)
    {
        try
        {
            response.EnsureSuccessStatusCode();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Response did not indicate success. {response.StatusCode}: {response.ReasonPhrase}", null, response.StatusCode);
            }

            var proxyId = GetRequestProxyId(request?.Uri);
            if (!String.IsNullOrEmpty(proxyId))
            {
                UpdateProxyRequestStatistics(proxyId, request.Uri, response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            var proxyId = GetRequestProxyId(request?.Uri);
            if (!String.IsNullOrEmpty(proxyId) && ex.StatusCode != null)
            {
                UpdateProxyRequestStatistics(proxyId, request?.Uri, ex.StatusCode.Value);
            }

            /*
            // Check if the content has not been modified since the last request
            // 304: Not Modified
            if (ex.IsNotModified)
            {
                throw new SteamNotModifiedException();
            }

            // Check if the request failed due to a temporary or network related error
            // 408: RequestTimeout
            // 504: GatewayTimeout
            // 502: BadGateway
            if (ex.IsTemporaryError)
            {
                _logger.LogWarning($"{ex.StatusCode} ({((int)ex.StatusCode)}), will retry...");
                return await GetWithRetry(request, (retryAttempt + 1));
            }

            // Check if the request failed due to rate limiting
            // 429: TooManyRequests
            if (ex.IsRateLimited)
            {
                // Add a cooldown to the current web proxy and rotate to the next proxy if possible
                // Steam web API terms of use (https://steamcommunity.com/dev/apiterms)
                //  - You are limited to one hundred thousand (100,000) calls to the Steam Web API per day.
                // Steam community web site rate-limits observed from personal testing:
                //  - You are limited to 25 requests within 30 seconds, which resets after ???.
                RotateWebProxyForHost(request?.Uri, cooldown: TimeSpan.FromMinutes(60));
                return await GetWithRetry(request, (retryAttempt + 1));
            }

            // Check if the request failed due to missing proxy authentication
            // 407: ProxyAuthenticationRequired
            if (ex.IsProxyAuthenticationRequired)
            {
                // Disable the current web proxy and rotate to the next proxy if possible
                DisableWebProxyForHost(request?.Uri);
                return await GetWithRetry(request, (retryAttempt + 1));
            }

            // Legitimate error, bubble the error up to the caller
            throw;
            */
        }

        return response;
    }

    protected string GetRequestProxyId(Uri requestAddress)
    {
        return (_webProxy as IRotatingWebProxy)?.GetProxyId(requestAddress);
    }

    protected void UpdateProxyRequestStatistics(string proxyId, Uri requestAddress, HttpStatusCode responseStatusCode)
    {
        (_webProxy as IRotatingWebProxy)?.UpdateProxyRequestStatistics(proxyId, requestAddress, responseStatusCode);
    }

    protected void CooldownWebProxyForHost(string proxyId, Uri host, TimeSpan cooldown)
    {
        (_webProxy as IRotatingWebProxy)?.CooldownProxy(proxyId, host, cooldown);
    }

    protected void DisableWebProxyForHost(string proxyId)
    {
        (_webProxy as IRotatingWebProxy)?.DisableProxy(proxyId);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _httpHandler?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public IDictionary<string, string> DefaultHeaders => _defaultHeaders;

    public CookieContainer Cookies => _cookieContainer;

    public TimeSpan? IfModifiedSinceTimeAgo { get; set; }

}
