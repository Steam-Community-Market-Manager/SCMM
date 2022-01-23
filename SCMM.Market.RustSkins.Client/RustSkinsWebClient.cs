using SCMM.Market.Client;
using System.Text.Json;

namespace SCMM.Market.RustSkins.Client
{
    public class RustSkinsWebClient : MarketWebClient
    {
        private const string BaseUri = "https://rustskins.com/api/v1/";

        public async Task<RustSkinsMarketListingsResponse> GetMarketListingsAsync(int page = 1)
        {
            using (var client = BuildHttpClient())
            {
                var url = $"{BaseUri}market/listings?sort=p-descending&page={page}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<RustSkinsMarketListingsResponse>(textJson);
                return responseJson;
            }
        }
    }
}
