using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SCMM.Market.SkinSerpent.Client
{
    public class SkinSerpentWebClient : Shared.Web.Client.WebClientBase
    {
        private const string WebsiteBaseUri = "https://skinserpent.com";

        public SkinSerpentWebClient(ILogger<SkinSerpentWebClient> logger) : base(logger)
        {
        }

        public async Task<SkinSerpentListingsResponse> GetListingsAsync(string appId, int page = 0)
        {
            using (var client = BuildWebApiHttpClient(host: new Uri(WebsiteBaseUri)))
            {
                var url = $"{WebsiteBaseUri}/api/listings/{appId}/?&category=&priceMin=0&priceMax=1000000&sortBy=P_DESC&search=&items=&colors=&page={page}";
                var response = await RetryPolicy.ExecuteAsync(() => client.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                return JsonSerializer.Deserialize<SkinSerpentListingsResponse>(textJson);
            }
        }
    }
}
