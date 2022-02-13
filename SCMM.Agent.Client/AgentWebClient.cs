using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;

namespace SCMM.Market.Client;

public class AgentWebClient : IDisposable
{
    private readonly CookieContainer _cookieContainer;
    private readonly HttpMessageHandler _httpHandler;
    private bool _disposedValue;

    public AgentWebClient(CookieContainer cookieContainer = null)
    {
        _cookieContainer = new CookieContainer();
        _httpHandler = new HttpClientHandler()
        {
            CookieContainer = _cookieContainer
        };
    }

    protected CookieContainer Cookies => _cookieContainer;

    protected HttpClient BuildHttpClient(bool disguisedAsWebBrowser = true, Uri referer = null)
    {
        var httpClient = new HttpClient(_httpHandler, false);
        if (disguisedAsWebBrowser)
        {
            // We are a normal looking web browser, honest (helps with WAF rules that block bots)
            httpClient.DefaultRequestHeaders.UserAgent.Clear();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(Windows NT 10.0; Win64; x64)"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AppleWebKit", "537.36"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(KHTML, like Gecko)"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Chrome", "96.0.4664.110"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Safari", "537.36"));

            // NOTE: Don't give them an easy way to identify us
            //DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SCMM", "1.0"));

            // We made this request from a web browser, honest (helps with WAF rules that enforce OWASP)
            httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        }

        // We made this request from your website, honest
        if (referer != null)
        {
            httpClient.DefaultRequestHeaders.Referrer = referer;
        }

        return httpClient;
    }

    protected ClientWebSocket BuildWebSocketClient()
    {
        var webSocketClient = new ClientWebSocket();
        webSocketClient.Options.Cookies = _cookieContainer;

        return webSocketClient;
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
