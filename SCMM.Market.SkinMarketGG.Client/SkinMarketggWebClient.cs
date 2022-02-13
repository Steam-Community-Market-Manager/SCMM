using SCMM.Market.Client;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SCMM.Market.SkinMarketgg.Client
{
    public class SkinMarketGGWebClient : AgentWebClient
    {
        private const string BaseUri = "https://api.skinmarket.gg/";

        public async Task<IEnumerable<SkinMarketGGItem>> GetTradeSiteInventoryAsync()
        {
            using (var client = GetHttpClient())
            {
                var url = $"{BaseUri}trade/site-inventory";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<IEnumerable<SkinMarketGGItem>>(textJson);
                return responseJson;
            }
        }
    }
}
