using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SCMM.Market.ShadowPay.Client
{
    public class ShadowPayWebClient : Shared.Web.Client.WebClientBase
    {
        private const string ApiBaseUri = "https://api.shadowpay.com/";

        private readonly ShadowPayConfiguration _configuration;

        public ShadowPayWebClient(ILogger<ShadowPayWebClient> logger, ShadowPayConfiguration configuration) : base(logger) 
        {
            _configuration = configuration;
        }

        /// <see cref="https://doc.shadowpay.com/docs/shadowpay/dbd310d5b59c1-get-item-prices"/>
        public async Task<IEnumerable<ShadowPayItem>> GetItemPricesAsync(string appName)
        {
            using (var client = BuildShadowPayClient())
            {
                var url = $"{ApiBaseUri}api/v2/merchant/items/prices?project={appName?.ToLower()}";
                var response = await RetryPolicy.ExecuteAsync(() => client.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                var responseJson = JsonSerializer.Deserialize<ShadowPayItemPricesResponse>(textJson);
                return responseJson?.Data;
            }
        }

        private HttpClient BuildShadowPayClient() => BuildWebApiHttpClient(
            authHeaderName: "Authorization",
            authHeaderFormat: "Bearer {0}",
            authKey: _configuration.ApiKey
        );
    }
}
