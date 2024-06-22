using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace SCMM.Market.RapidSkins.Client
{
    public class RapidSkinsWebClient : Shared.Web.Client.WebClientBase
    {
        private const string WebBaseUri = "https://rapidskins.com/";

        public RapidSkinsWebClient(ILogger<RapidSkinsWebClient> logger) : base(logger) { }

        // TODO: Implement new API, https://api.rapidskins.com/graphql
        [Obsolete("This no longer works, API has been removed")]
        public async Task<RapidSkinsInventoryResponse> GetInventoryAsync()
        {
            using (var client = BuildWebBrowserHttpClient(referrer: new Uri(WebBaseUri)))
            {
                var url = $"{WebBaseUri}api/v1/site/inventory";
                var response = await RetryPolicy.ExecuteAsync(() => client.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                var responseJson = JsonSerializer.Deserialize<RapidSkinsInventoryResponse>(textJson);
                return responseJson;
            }
        }
    }
}
