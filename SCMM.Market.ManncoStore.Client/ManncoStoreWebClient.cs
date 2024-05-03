using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SCMM.Market.ManncoStore.Client
{
    public class ManncoStoreWebClient : Shared.Web.Client.WebClientBase
    {
        private const string WebsiteBaseUri = "https://mannco.store/";

        public ManncoStoreWebClient(ILogger<ManncoStoreWebClient> logger) : base(logger) 
        {
        }

        public async Task<IEnumerable<ManncoStoreItem>> GetItemsAsync(string appId, int skip = 0)
        {
            using (var client = BuildWebBrowserHttpClient())
            {
                var url = $"{WebsiteBaseUri}items/get?price=DESC&page=1&i=0&game={appId}&skip={skip}";
                var response = await RetryPolicy.ExecuteAsync(() => client.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                var responseJson = JsonSerializer.Deserialize<ManncoStoreItem[]>(textJson);
                return responseJson;
            }
        }
    }
}
