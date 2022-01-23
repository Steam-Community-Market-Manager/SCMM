using SCMM.Market.Client;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SCMM.Market.DMarket.Client
{
    public class DMarketWebClient
    {
        private const string BaseUri = "https://api.dmarket.com/exchange/v1/";

        public async Task<DMarketMarketItemsResponse> GetMarketItemsAsync(string appName, string currencyName = "USD", string cursor = null, int limit = 100)
        {
            using (var client = new MarketHttpClient())
            {
                var url = $"{BaseUri}market/items?side=market&orderBy=price&orderDir=desc&priceFrom=0&priceTo=0&treeFilters=&gameId={appName}&types=dmarket&cursor={cursor}&limit={limit}&currency={currencyName}&platform=browser&isLoggedIn=true";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<DMarketMarketItemsResponse>(textJson);
                return responseJson;
            }
        }
    }
}
