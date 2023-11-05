using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace SCMM.Market.SkinsMonkey.Client
{
    public class SkinsMonkeyWebClient : Shared.Web.Client.WebClientBase
    {
        private const string ApiBaseUri = "https://skinsmonkey.com/api/public/v1/";

        private readonly SkinsMonkeyConfiguration _configuration;

        public SkinsMonkeyWebClient(ILogger<SkinsMonkeyWebClient> logger, SkinsMonkeyConfiguration configuration, IWebProxy webProxy) : base(logger, webProxy: webProxy)
        {
            _configuration = configuration;
        }

        public async Task<IEnumerable<SkinsMonkeyItem>> GetItemPricesAsync(string appId)
        {
            using (var client = BuildSkinsMoneyClient())
            {
                var url = $"{ApiBaseUri}price/{Uri.EscapeDataString(appId)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

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
