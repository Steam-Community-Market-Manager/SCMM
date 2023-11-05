using Microsoft.Extensions.Logging;
using SCMM.Steam.Data.Models;
using System.Net;
using System.Text.Json;

namespace SCMM.Market.RustTM.Client
{
    public class RustTMWebClient : Shared.Web.Client.WebClientBase
    {
        private const string ApiBaseUri = "https://rust.tm/api/v2/";

        public RustTMWebClient(ILogger<RustTMWebClient> logger, IWebProxy webProxy) : base(logger, webProxy: webProxy) { }

        public async Task<IEnumerable<RustTMItem>> GetPricesAsync(string currencyName = Constants.SteamCurrencyUSD)
        {
            using (var client = BuildWebApiHttpClient())
            {
                var url = $"{ApiBaseUri}prices/{currencyName.ToUpper()}.json";
                var response = await RetryPolicy.ExecuteAsync(() => client.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                var responseJson = JsonSerializer.Deserialize<RustTMPricesResponse>(textJson);
                return responseJson?.Items;
            }
        }
    }
}
