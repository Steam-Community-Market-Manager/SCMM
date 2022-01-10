using System.Text.Json;

namespace SCMM.Market.CSDeals.Client
{
    public class CSDealsWebClient
    {
        private const string BaseUri = "https://cs.deals/";
        private const string BaseApiUri = "https://cs.deals/API/";

        public async Task<IEnumerable<CSDealsItemPrice>> PricingGetLowestPricesAsync(string appId)
        {
            using (var client = new HttpClient())
            {
                var url = $"{BaseApiUri}IPricing/GetLowestPrices/v1?appid={Uri.EscapeDataString(appId)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<CSDealsResponse<CSDealsPricingGetLowestPricesResult>>(textJson);
                return responseJson?.Response?.Items;
            }
        }

        public async Task<CSDealsMarketplaceSearchResults<CSDealsAppItems>> MarketplaceSearchAsync(string appId, int page = 0)
        {
            using (var client = new HttpClient())
            {
                var url = $"{BaseUri}marketplace-search";
                var payload = new FormUrlEncodedContent(new Dictionary<string, string>() {
                    { "appid", appId },
                    { "page", page.ToString() }
                });

                client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                var response = await client.PostAsync(url, payload);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<CSDealsResponse<CSDealsMarketplaceSearchResults<CSDealsAppItems>>>(textJson);
                return responseJson?.Response;
            }
        }

        public async Task<CSDealsBotsInventoryResult> BotsInventoryAsync(string appId)
        {
            using (var client = new HttpClient())
            {
                var url = $"{BaseUri}botsinventory";
                var payload = new FormUrlEncodedContent(new Dictionary<string, string>() {
                    { "appid", appId }
                });

                client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                var response = await client.PostAsync(url, payload);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<CSDealsResponse<CSDealsBotsInventoryResult>>(textJson);
                return responseJson?.Response;
            }
        }
    }
}
