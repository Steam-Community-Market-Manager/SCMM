using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SCMM.Market.SnipeSkins.Client
{
    public class SnipeSkinsWebClient : Shared.Web.Client.WebClientBase
    {
        private const string WebsiteBaseUri = "https://snipeskins.com";

        public SnipeSkinsWebClient(ILogger<SnipeSkinsWebClient> logger) : base(logger)
        {
        }

        public async Task<SnipeSkinsPricesResponse> GetPricesAsync(string appId)
        {
            using (var client = BuildWebApiHttpClient(host: new Uri(WebsiteBaseUri)))
            {
                var url = $"{WebsiteBaseUri}/api/v1.1/market/{appId}/prices";
                var response = await RetryPolicy.ExecuteAsync(() => client.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                return JsonSerializer.Deserialize<SnipeSkinsPricesResponse>(textJson);
            }
        }
    }
}
