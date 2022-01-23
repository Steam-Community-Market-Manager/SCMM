using SCMM.Market.Client;
using System.Text.Json;

namespace SCMM.Market.SkinsMonkey.Client
{
    public class SkinsMonkeyWebClient
    {
        private const string BaseUri = "https://skinsmonkey.com/api/";

        public async Task<IEnumerable<SkinsMonkeyItemListing>> GetInventoryAsync(string appId, int offset = 0, int limit = 1000)
        {
            using (var client = new MarketHttpClient())
            {
                var url = $"{BaseUri}inventory?appId={Uri.EscapeDataString(appId)}&offset={offset}&limit={limit}&sort=price-desc&force=true";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<SkinsMonkeyInventoryResponse>(textJson);
                return responseJson?.Assets;
            }
        }
    }
}
