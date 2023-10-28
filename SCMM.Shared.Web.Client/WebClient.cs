using System.Net;
using System.Net.Http.Headers;

namespace SCMM.Shared.Web.Client;

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

        var httpClientManager = new HttpClientHandler()
        {
            UseCookies = cookieContainer != null,
            CookieContainer = cookieContainer ?? new CookieContainer(),
            Proxy = webProxy,
            PreAuthenticate = webProxy != null,
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 3,
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (_webProxy == null) ? null :
                // Http web proxy might MiTM the SSL certificate, so ignore invalid certs when using a proxy
                (httpRequestMessage, cert, cetChain, policyErrors) => true
        };

        _httpHandler = (_webProxy != null)
            ? new WebProxyAwareHttpHandler(_webProxy as IWebProxyManager, () => RateLimitCooldown, httpClientManager)
            : httpClientManager;
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
                httpClient.DefaultRequestHeaders.Add(authHeaderName, string.Format(authHeaderFormat, authKey));
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

    /// <summary>
    /// If set, the server may respond with a 304 response if the resource has not been modified within the specified time frame.
    /// </summary>
    public TimeSpan? IfModifiedSinceTimeAgo { get; set; }

    /// <summary>
    /// If a request temporarily fails, how many times should it be retried before permanently failing.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// When web proxies are being used, how long should the proxy be put in cool down for if it gets rate limited.
    /// </summary>
    public TimeSpan? RateLimitCooldown { get; set; } = TimeSpan.FromMinutes(15);
}
