using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SCMM.Market.SkinSwap.Client
{
    public class SkinSwapWebClient : Shared.Client.WebClient
    {
        private const string ApiBaseUri = "https://skinswap.com/api";

        private readonly SkinSwapConfiguration _configuration;

        public SkinSwapWebClient(SkinSwapConfiguration configuration, IWebProxy webProxy) : base(webProxy: webProxy)
        {
            _configuration = configuration;
            DefaultHeaders.Add("Accept", "application/json");
        }

        public async Task<IEnumerable<SkinSwapItem>> GetItemsAsync()
        {
            using (var client = BuildSkinsSwapClient())
            {
                var url = $"{ApiBaseUri}/v1/items";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<SkinSwapResponse<SkinSwapItem[]>>(textJson);
                return responseJson?.Data;
            }
        }

        private HttpClient BuildSkinsSwapClient() => BuildWebApiHttpClient(
            authHeaderName: "Authorization",
            authHeaderFormat: "Bearer {0}",
            authKey: _configuration.ApiKey
        );
    }
}
