using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace SCMM.Market.iTradegg.Client
{
    public class iTradeggWebClient : Shared.Web.Client.WebClientBase
    {
        private const string WebBaseUri = "https://itrade.gg/";

        public iTradeggWebClient(ILogger<iTradeggWebClient> logger) : base(logger) { }

        // TODO: Implement new API, https://api.itrade.gg/inventory/bot/rust-data
        [Obsolete("This no longer works, API has been removed")]
        public async Task<IEnumerable<iTradeggItem>> GetInventoryAsync(string appId)
        {
            using (var client = BuildWebBrowserHttpClient(referrer: new Uri(WebBaseUri)))
            {
                var url = $"{WebBaseUri}ajax/getInventory?game={appId}&type=bot";
                var response = await RetryPolicy.ExecuteAsync(() => client.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                var responseJson = JsonSerializer.Deserialize<iTradeggInventoryResponse>(textJson);
                return responseJson?.Inventory?.Items?.Select(x => x.Value);
            }
        }
    }
}
