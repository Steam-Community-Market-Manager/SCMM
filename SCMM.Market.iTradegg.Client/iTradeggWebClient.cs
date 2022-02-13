using SCMM.Market.Client;
using System.Text.Json;

namespace SCMM.Market.iTradegg.Client
{
    public class iTradeggWebClient : AgentWebClient
    {
        private const string BaseUri = "https://itrade.gg/ajax/";

        public async Task<IEnumerable<iTradeggItem>> GetInventoryAsync(string appId)
        {
            using (var client = GetHttpClient())
            {
                var url = $"{BaseUri}getInventory?game={appId}&type=bot";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<iTradeggInventoryResponse>(textJson);
                return responseJson?.Inventory?.Items?.Select(x => x.Value);
            }
        }
    }
}
