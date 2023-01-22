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
            MaxAutomaticRedirections = 3
        };
    }
    
    protected HttpClient BuildWebBrowserHttpClient(Uri referer = null)
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

        if (referer != null)
        {
            // We made this request from your website, honest...
            httpClient.DefaultRequestHeaders.Referrer = referer;
        }

        return httpClient;
    }

    protected HttpClient BuildWebApiHttpClient()
    {
        return BuildWebApiHttpClient(null, null, null);
    }

    protected HttpClient BuildWebApiHttpClient(string authHeaderName, string authHeaderFormat, string authKey = null)
    {
        var httpClient = BuildHttpClient();
        if (!string.IsNullOrEmpty(authHeaderName) && !string.IsNullOrEmpty(authHeaderFormat) && !string.IsNullOrEmpty(authKey))
        {
            if (!httpClient.DefaultRequestHeaders.Contains(authHeaderName))
            {
                httpClient.DefaultRequestHeaders.Add(authHeaderName, String.Format(authHeaderFormat, authKey));
            }
        }

        return httpClient;
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
        /*
        if (httpClient.DefaultRequestHeaders.IfModifiedSince == null && IfModifiedSinceTimeAgo != null)
        {
            httpClient.DefaultRequestHeaders.IfModifiedSince = DateTimeOffset.UtcNow.Subtract(IfModifiedSinceTimeAgo.Value);
        }
        */
        return httpClient;
    }

    public void UpdateRequestStatistics(Uri address, HttpStatusCode responseStatusCode)
    {
        (_webProxy as IRotatingWebProxy)?.UpdateRequestStatistics(address, responseStatusCode);
    }

    public void RotateWebProxy(Uri address, TimeSpan cooldown)
    {
        (_webProxy as IRotatingWebProxy)?.RotateProxy(address, cooldown);
    }

    public void DisableWebProxy(Uri address)
    {
        (_webProxy as IRotatingWebProxy)?.DisableProxy(address);
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
