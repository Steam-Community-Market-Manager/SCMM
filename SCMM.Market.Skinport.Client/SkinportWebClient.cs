using SCMM.Market.Client;
using System.Text.Json;

namespace SCMM.Market.Skinport.Client
{
    public class SkinportWebClient : AgentWebClient
    {
        private const string BaseUri = "https://api.skinport.com/v1/";

        public async Task<IEnumerable<SkinportItem>> GetItemsAsync(string appId, string currency = null)
        {
            using (var client = GetHttpClient())
            {
                var url = $"{BaseUri}items?app_id={Uri.EscapeDataString(appId)}&currency={Uri.EscapeDataString(currency)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<SkinportItem[]>(textJson);
                return responseJson;
            }
        }
    }
}
