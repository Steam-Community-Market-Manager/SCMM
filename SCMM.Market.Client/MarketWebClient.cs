using System.Net;
using System.Net.Http.Headers;

namespace SCMM.Market.Client;

public class MarketWebClient : IDisposable
{
    private readonly CookieContainer _cookieContainer;
    private readonly HttpMessageHandler _httpHandler;
    private bool _disposedValue;

    public MarketWebClient(CookieContainer cookieContainer = null)
    {
        _cookieContainer = new CookieContainer();
        _httpHandler = new HttpClientHandler()
        {
            CookieContainer = _cookieContainer
        };
    }

    protected CookieContainer Cookies => _cookieContainer;

    protected HttpClient BuildHttpClient(Uri referer = null)
    {
        var httpClient = new HttpClient(_httpHandler, false);

        // NOTE: Most markets use CloudFlare anti-bot protection and/or request filtering with that block clients that don't advertise sane look user agent strings
        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(Windows NT 10.0; Win64; x64)"));
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AppleWebKit", "537.36"));
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(KHTML, like Gecko)"));
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Chrome", "96.0.4664.110"));
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Safari", "537.36"));

        // NOTE: We don't want to give them an easy way to identify and block us...
        //DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SCMM", "1.0"));

        // Most markets require this
        httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

        // Some markets require this
        if (referer != null)
        {
            httpClient.DefaultRequestHeaders.Referrer = referer;
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
}
