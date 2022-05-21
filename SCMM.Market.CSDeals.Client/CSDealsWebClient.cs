using SCMM.Worker.Client;
using System.Text.Json;

namespace SCMM.Market.CSDeals.Client
{
    public class CSDealsWebClient : Worker.Client.WebClient
    {
        private const string BaseUri = "https://cs.deals/";
        private const string BaseApiUri = "https://cs.deals/API/";

        public async Task<IEnumerable<CSDealsItemPrice>> GetPricingGetLowestPricesAsync(string appId)
        {
            using (var client = BuildWebBrowserHttpClient())
            {
                var url = $"{BaseApiUri}IPricing/GetLowestPrices/v1?appid={Uri.EscapeDataString(appId)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<CSDealsResponse<CSDealsPricingGetLowestPricesResult>>(textJson);
                return responseJson?.Response?.Items;
            }
        }

        public async Task<CSDealsMarketplaceSearchResults<CSDealsItemListings>> PostMarketplaceSearchAsync(string appId, int page = 0)
        {
            using (var client = BuildWebBrowserHttpClient())
            {
                var url = $"{BaseUri}ajax/marketplace-search";
                var payload = new FormUrlEncodedContent(new Dictionary<string, string>() {
                    { "appid", appId },
                    { "page", page.ToString() }
                });

                var response = await client.PostAsync(url, payload);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<CSDealsResponse<CSDealsMarketplaceSearchResults<CSDealsItemListings>>>(textJson);
                return responseJson?.Response;
            }
        }

        public async Task<CSDealsBotsInventoryResult> PostBotsInventoryAsync(string appId)
        {
            using (var client = BuildWebBrowserHttpClient())
            {
                var url = $"{BaseUri}ajax/botsinventory";
                var payload = new FormUrlEncodedContent(new Dictionary<string, string>() {
                    { "appid", appId }
                });

                var response = await client.PostAsync(url, payload);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<CSDealsResponse<CSDealsBotsInventoryResult>>(textJson);
                return responseJson?.Response;
            }
        }
    }
}
