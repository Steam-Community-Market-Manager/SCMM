using SCMM.Market.Client;
using System.Text.Json;

namespace SCMM.Market.SwapGG.Client
{
    public class SwapGGWebClient : MarketWebClient
    {
        private const string TradeBaseUri = "https://api.swap.gg/";
        private const string MarketBaseUri = "https://market-api.swap.gg/v1/";

        public async Task<IEnumerable<SwapGGTradeItem>> GetTradeBotInventoryAsync(string appId)
        {
            using (var client = BuildHttpClient())
            {
                var url = $"{TradeBaseUri}inventory/bot/{Uri.EscapeDataString(appId)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<SwapGGResponse<IEnumerable<SwapGGTradeItem>>>(textJson);
                return responseJson?.Result;
            }
        }

        public async Task<IDictionary<string, SwapGGMarketItem>> GetMarketPricingLowestAsync(string appId)
        {
            using (var client = BuildHttpClient())
            {
                var url = $"{MarketBaseUri}pricing/lowest?appId={Uri.EscapeDataString(appId)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<SwapGGResponse<IDictionary<string, SwapGGMarketItem>>>(textJson);
                return responseJson?.Result;
            }
        }
    }
}
