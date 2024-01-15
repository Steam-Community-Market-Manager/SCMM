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

        /// <see cref="https://doc.shadowpay.com/docs/shadowpay/96108be6ddc1e-get-items-on-sale"/>
        public async Task<IEnumerable<ManncoStoreItem>> GetItemsAsync(ulong appId, int skip = 0)
        {
            using (var client = BuildWebBrowserHttpClient(referrer: new Uri(WebsiteBaseUri)))
            {
                var url = $"{WebsiteBaseUri}items?a=&b=&c=&d=&e=&f=DESC&g=&h=1&i=0&game={appId}&j=1&k=&l=&m=&n=&o=&s=&t=&skip={skip}";
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
