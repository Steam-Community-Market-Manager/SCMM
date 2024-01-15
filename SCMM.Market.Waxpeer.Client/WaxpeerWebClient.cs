using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SCMM.Market.Waxpeer.Client
{
    public class WaxpeerWebClient : Shared.Web.Client.WebClientBase
    {
        private const string ApiBaseUri = "https://api.waxpeer.com/";

        public WaxpeerWebClient(ILogger<WaxpeerWebClient> logger) : base(logger) 
        {
        }

        /// <see cref="https://docs.waxpeer.com/?method=v1-prices-get"/>
        public async Task<WaxpeerPricesResponse> GetPricesAsync(string appName)
        {
            using (var client = BuildWebApiHttpClient())
            {
                var url = $"{ApiBaseUri}v1/prices/items?game={appName.ToLower()}t&minified=1";
                var response = await RetryPolicy.ExecuteAsync(() => client.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                var responseJson = JsonSerializer.Deserialize<WaxpeerPricesResponse>(textJson);
                return responseJson;
            }
        }
    }
}
