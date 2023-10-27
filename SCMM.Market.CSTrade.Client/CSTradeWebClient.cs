using System.Net;
using System.Text.Json;

namespace SCMM.Market.CSTrade.Client
{
    public class CSTradeWebClient : Shared.Web.Client.WebClient
    {
        private const string PricesApiBaseUri = "https://cdn.cs.trade:2096/api/";
        private const string WebsiteApiBaseUri = "https://cdn.cs.trade:8443/api/";

        public CSTradeWebClient(IWebProxy webProxy) : base(webProxy: webProxy) { }

        /// <summary>
        /// 
        /// </summary>
        /// <see cref="https://cdn.cs.trade:2096/api/prices_RUST"/>
        /// <returns></returns>
        public async Task<IEnumerable<CSTradeItemPrice>> GetPricesAsync(string appName)
        {
            using (var client = BuildWebApiHttpClient())
            {
                var url = $"{PricesApiBaseUri}prices_{appName}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<IDictionary<string, CSTradeItemPrice>>(textJson);
                return responseJson?.Select(x =>
                    {
                        x.Value.Name = x.Key;
                        return x.Value;
                    })
                    .ToArray();
            }
        }

        public async Task<IEnumerable<CSTradeInventoryItem>> GetInventoryAsync()
        {
            using (var client = BuildWebApiHttpClient())
            {
                var url = $"{WebsiteApiBaseUri}getInventory?order_by=price_desc&bot=all";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<CSTradeInventoryResponse>(textJson);
                return responseJson?.Inventory;
            }
        }
    }
}
