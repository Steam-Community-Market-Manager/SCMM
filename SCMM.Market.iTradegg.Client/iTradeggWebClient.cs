using System.Net;
using System.Text.Json;

namespace SCMM.Market.iTradegg.Client
{
    public class iTradeggWebClient : Shared.Client.WebClient
    {
        private const string WebBaseUri = "https://itrade.gg/";

        public iTradeggWebClient(IWebProxy webProxy) : base(webProxy: webProxy) { }

        public async Task<IEnumerable<iTradeggItem>> GetInventoryAsync(string appId)
        {
            using (var client = BuildWebBrowserHttpClient(referer: new Uri(WebBaseUri)))
            {
                var url = $"{WebBaseUri}ajax/getInventory?game={appId}&type=bot";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<iTradeggInventoryResponse>(textJson);
                return responseJson?.Inventory?.Items?.Select(x => x.Value);
            }
        }
    }
}
