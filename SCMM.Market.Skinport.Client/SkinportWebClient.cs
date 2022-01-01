using System.Text.Json;

namespace SCMM.Market.Skinport.Client
{
    public class SkinportWebClient
    {
        private const string BaseUri = "https://skinport.com/api/";

        public async Task<IEnumerable<SkinportMarketItem>> BrowseMarketItemsAsync(string appId, string itemName)
        {
            using (var client = new HttpClient())
            {
                var url = $"{BaseUri}browse/{Uri.EscapeDataString(appId)}?item={Uri.EscapeDataString(itemName)}&sort=price&order=asc";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<SkinportMarketBrowseResponseJson>(textJson);
                return responseJson?.Items;
            }
        }
    }
}
