using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SCMM.Market.ShadowPay.Client
{
    public class ShadowPayWebClient : Shared.Web.Client.WebClientBase
    {
        private const string WebsiteBaseUri = "https://shadowpay.com/";
        private const string ApiBaseUri = "https://api.shadowpay.com/";

        public ShadowPayWebClient(ILogger<ShadowPayWebClient> logger) : base(logger) 
        {
        }

        /// <see cref="https://doc.shadowpay.com/docs/shadowpay/96108be6ddc1e-get-items-on-sale"/>
        public async Task<IEnumerable<ShadowPayItem>> GetItemsAsync(string appName, int offset, int limit)
        {
            using (var client = BuildWebBrowserHttpClient(referrer: new Uri(WebsiteBaseUri)))
            {
                var url = $"{ApiBaseUri}api/market/get_items?currency=USD&sort_column=price&sort_dir=asc&stack=false&offset={offset}&limit={limit}&sort=asc&game=rust";
                var response = await RetryPolicy.ExecuteAsync(() => client.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                var responseJson = JsonSerializer.Deserialize<ShadowPayItemsResponse>(textJson);
                return responseJson?.Data;
            }
        }
    }
}
