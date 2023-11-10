using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace SCMM.Market.CSTrade.Client
{
    public class CSTradeWebClient : Shared.Web.Client.WebClientBase
    {
        private const string WebsiteBaseUri = "https://cs.trade/";
        private const string PricesApiBaseUri = "https://cdn.cs.trade:2096/api/";
        private const string WebsiteApiBaseUri = "https://cdn.cs.trade:8443/api/";

        public CSTradeWebClient(ILogger<CSTradeWebClient> logger) : base(logger) { }

        /// <summary>
        /// 
        /// </summary>
        /// <see cref="https://cdn.cs.trade:2096/api/prices_RUST"/>
        /// <returns></returns>
        public async Task<IEnumerable<CSTradeItemPrice>> GetPricesAsync(string appName)
        {
            using (var client = BuildWebBrowserHttpClient(referrer: new Uri(WebsiteBaseUri)))
            {
                var url = $"{PricesApiBaseUri}prices_{appName}";
                var response = await RetryPolicy.ExecuteAsync(() => client.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                var responseJson = JsonSerializer.Deserialize<IDictionary<string, CSTradeItemPrice>>(textJson);
                return responseJson?.Select(x =>
                    {
                        x.Value.Name = x.Key;
                        return x.Value;
                    })
                    .ToArray();
            }
        }
    }
}
