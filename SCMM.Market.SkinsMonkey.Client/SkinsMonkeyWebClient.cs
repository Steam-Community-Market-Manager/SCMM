using SCMM.Market.Client;
using System.Text.Json;

namespace SCMM.Market.SkinsMonkey.Client
{
    public class SkinsMonkeyWebClient : AgentWebClient
    {
        private const string ApiUri = "https://skinsmonkey.com/api/public/v1/";
        private const string ApiKey = "8Hcug9zVDBecchN82H629CZ3Wqt6YmRc";

        public const int MaxPageLimit = 120;

        public async Task<IEnumerable<SkinsMonkeyItem>> GetItemPricesAsync(string appId)
        {
            using (var client = GetHttpClient(disguisedAsWebBrowser: false, apiKey: ApiKey))
            {
                var url = $"{ApiUri}price/{Uri.EscapeDataString(appId)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<SkinsMonkeyItem>>(textJson);
            }
        }
    }
}
