using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace SCMM.Market.SkinSwap.Client
{
    public class SkinSwapWebClient : Shared.Web.Client.WebClientBase
    {
        private const string ApiBaseUri = "https://skinswap.com/api";

        private readonly SkinSwapConfiguration _configuration;

        public SkinSwapWebClient(ILogger<SkinSwapWebClient> logger, SkinSwapConfiguration configuration) : base(logger)
        {
            _configuration = configuration;
            DefaultHeaders.Add("Accept", "application/json");
        }

        public async Task<IEnumerable<SkinSwapItem>> GetItemAsync()
        {
            using (var client = BuildSkinsSwapClient())
            {
                var url = $"{ApiBaseUri}/v1/items";
                var response = await RetryPolicy.ExecuteAsync(() => client.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

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
