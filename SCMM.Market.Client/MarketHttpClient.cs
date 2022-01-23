using System.Net.Http.Headers;

namespace SCMM.Market.Client;

public class MarketHttpClient : HttpClient
{
    public MarketHttpClient() : base()
    {
        // NOTE: Most markets use CloudFlare anti-bot protection and/or request filtering with that block clients that don't advertise sane look user agent strings
        DefaultRequestHeaders.UserAgent.Clear();
        DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
        DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(Windows NT 10.0; Win64; x64)"));
        DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AppleWebKit", "537.36"));
        DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(KHTML, like Gecko)"));
        DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Chrome", "96.0.4664.110"));
        DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Safari", "537.36"));

        // NOTE: We don't want to give them an easy way to identify and block us...
        //DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SCMM", "1.0"));
    }
}
