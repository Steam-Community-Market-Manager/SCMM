using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace SCMM.Market.Buff.Client
{
    public class BuffWebClient : Shared.Web.Client.WebClientBase
    {
        private const string WebBaseUri = "https://buff.163.com/";
        private const string ApiBaseUri = "https://buff.163.com/api/";

        public const int MaxPageLimit = 80;

        public BuffWebClient(ILogger<BuffWebClient> logger, BuffConfiguration configuration, IWebProxy webProxy) : base(logger, cookieContainer: new CookieContainer(), webProxy: webProxy)
        {
            Cookies.Add(new Uri(ApiBaseUri), new Cookie("session", configuration.SessionId));
        }

        public async Task<BuffMarketGoodsResponse> GetMarketGoodsAsync(string appName, int page = 1, int pageSize = MaxPageLimit)
        {
            using (var client = BuildWebBrowserHttpClient(referrer: new Uri($"{WebBaseUri}/market/{appName?.ToLower()}")))
            {
                var url = $"{ApiBaseUri}market/goods?game={appName?.ToLower()}&page_num={page}&page_size={pageSize}&sort_by=price.desc&trigger=undefined_trigger&_={Random.Shared.NextInt64(1000000000000L, 9999999999999L)}";
                var response = await RetryPolicy.ExecuteAsync(() => client.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                var responseJson = JsonSerializer.Deserialize<BuffResponse<BuffMarketGoodsResponse>>(textJson);
                return responseJson?.Data;
            }
        }
    }
}
