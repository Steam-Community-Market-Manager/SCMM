using System.Text.Json;

namespace SCMM.Market.CSDeals.Client
{
    public class CSDealsWebClient
    {
        private const string BaseUri = "https://cs.deals/API/";

        public async Task<IEnumerable<CSDealsItemPrice>> PricingGetLowestPricesAsync(string appId)
        {
            using (var client = new HttpClient())
            {
                var url = $"{BaseUri}IPricing/GetLowestPrices/v1?appid={Uri.EscapeDataString(appId)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<CSDealsResponse<CSDealsPricingGetLowestPricesResponse>>(textJson);
                return responseJson?.Response?.Items;
            }
        }
    }
}
