using SCMM.Worker.Client;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SCMM.Market.SkinMarketgg.Client
{
    public class SkinMarketGGWebClient : Worker.Client.WebClient
    {
        private const string BaseUri = "https://api.skinmarket.gg/";

        public async Task<IEnumerable<SkinMarketGGItem>> GetTradeSiteInventoryAsync()
        {
            using (var client = BuildWebBrowserHttpClient())
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
