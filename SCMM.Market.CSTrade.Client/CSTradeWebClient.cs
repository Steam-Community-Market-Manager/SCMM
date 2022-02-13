using SCMM.Market.Client;
using System.Text.Json;

namespace SCMM.Market.CSTrade.Client
{
    public class CSTradeWebClient : AgentWebClient
    {
        private const string BaseUri = "https://cdn.cs.trade:8443/api/";

        public async Task<IEnumerable<CSTradeItem>> GetInventoryAsync()
        {
            using (var client = BuildHttpClient())
            {
                var url = $"{BaseUri}getInventory?order_by=price_desc&bot=all";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<CSTradeInventoryResponse>(textJson);
                return responseJson?.Inventory;
            }
        }
    }
}
