using System.Net;
using System.Text.Json;

namespace SCMM.Market.SkinsMonkey.Client
{
    public class SkinsMonkeyWebClient : Shared.Client.WebClient
    {
        private const string ApiUri = "https://skinsmonkey.com/api/public/v1/";

        private readonly SkinsMonkeyConfiguration _configuration;

        public SkinsMonkeyWebClient(IWebProxy webProxy) : base(webProxy: webProxy) { }

        public SkinsMonkeyWebClient(SkinsMonkeyConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IEnumerable<SkinsMonkeyItem>> GetItemPricesAsync(string appId)
        {
            using (var client = BuildSkinsMoneyClient())
            {
                var url = $"{ApiUri}price/{Uri.EscapeDataString(appId)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<SkinsMonkeyItem>>(textJson);
            }
        }

        private HttpClient BuildSkinsMoneyClient() => BuildWebApiHttpClient(
            authHeaderName: "x-api-key",
            authHeaderFormat: "{0}",
            authKey: _configuration.ApiKey
        );
    }
}
