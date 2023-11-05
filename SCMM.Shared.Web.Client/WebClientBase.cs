using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Authentication;

namespace SCMM.Shared.Web.Client;

public abstract class WebClientBase : IDisposable
{
    private readonly ILogger _logger;
    private readonly IDictionary<string, string> _defaultHeaders;
    private readonly CookieContainer _cookieContainer;
    private readonly IWebProxy _webProxy;
    private readonly HttpMessageHandler _httpHandler;
    private bool _disposedValue;

    private readonly AsyncRetryPolicy<HttpResponseMessage> _asyncRetryPolicy;

    protected WebClientBase(ILogger logger, HttpMessageHandler httpHandler = null, CookieContainer cookieContainer = null, IWebProxy webProxy = null)
    {
        _logger = logger;
        _defaultHeaders = new Dictionary<string, string>();
        _cookieContainer = cookieContainer;
        _webProxy = webProxy;

        httpHandler ??= new HttpClientHandler()
        {
            UseCookies = cookieContainer != null,
            CookieContainer = cookieContainer ?? new CookieContainer(),
            Proxy = webProxy,
            PreAuthenticate = webProxy != null,
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (_webProxy == null) ? null :
                // Http web proxy might MiTM the SSL certificate, so ignore invalid certs when using a proxy
                (httpRequestMessage, cert, cetChain, policyErrors) => true
        };

        _httpHandler = (_webProxy is IWebProxyManager webProxyManager)
            ? new WebProxyAwareHttpMessageHandler(logger, webProxyManager, () => RateLimitCooldown, httpHandler)
            : httpHandler;

        _asyncRetryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrInner<AuthenticationException>()
            .OrInner<TimeoutException>()
            .OrResult(x => x.StatusCode == HttpStatusCode.ProxyAuthenticationRequired)
            .OrResult(x => x.StatusCode == HttpStatusCode.RequestTimeout)
            .OrResult(x => x.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(MaxRetries, retryAttempt => TimeSpan.FromSeconds(retryAttempt), (result, timeSpan, retryCount, context) =>
            {
                if (result.Result != null)
                {
                    _logger.LogWarning($"Transient http request failure, {result.Result.ReasonPhrase?.ToLower() ?? "no reason specified"} (id: {context.CorrelationId}, attempt: {retryCount}, httpStatusCode: {(int)result.Result.StatusCode})'");
                }
                else if (result.Exception != null)
                {
                    _logger.LogWarning(result.Exception, $"Transient http request failure, {result.Exception.Message} (id: {context.CorrelationId}, attempt: {retryCount})'");
                }
            });
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

        // Add user agent browser hint headers (helps with bypassing WAF rules that block bots/browsers)
        if (!httpClient.DefaultRequestHeaders.Any(x => x.Key.StartsWith("Sec")))
        {
            httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua", @"""Chromium"";v=""118"", ""Brave"";v=""118"", ""Not=A?Brand"";v=""99""");
            httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?1");
            httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", @"""Android""");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            httpClient.DefaultRequestHeaders.Add("Sec-Gpc", "1");
        }

        // Add user agent header (helps with bypassing WAF rules that block bots/browsers)
        if (!httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(Windows NT 10.0; Win64; x64)"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AppleWebKit", "537.36"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(KHTML, like Gecko)"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Chrome", "118.0.0.0"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Safari", "537.36"));
        }

        // Act like a web browser (helps with bypassing WAF rules that enforce OWASP rules or have CSRF protection)
        if (!httpClient.DefaultRequestHeaders.Contains("X-Requested-With"))
        {
            httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        }

        // Act like we are from your web page (helps with bypassing WAF rules)
        if (referrer != null)
        {
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

    protected AsyncPolicy<HttpResponseMessage> RetryPolicy => _asyncRetryPolicy;

    public IDictionary<string, string> DefaultHeaders => _defaultHeaders;

    public CookieContainer Cookies => _cookieContainer;

    /// <summary>
    /// If a request temporarily fails, how many times should it be retried before permanently failing.
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// When web proxies are being used, how long should the proxy be put in cool down for if it gets rate limited.
    /// </summary>
    public TimeSpan? RateLimitCooldown { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// If set, the server may respond with a 304 response if the resource has not been modified within the specified time frame.
    /// </summary>
    public TimeSpan? IfModifiedSinceTimeAgo { get; set; }
}
