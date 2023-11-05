using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace SCMM.Market.LootFarm.Client
{
    public class LootFarmWebClient : Shared.Web.Client.WebClientBase
    {
        private const string BaseUri = "https://loot.farm/";

        public LootFarmWebClient(ILogger<LootFarmWebClient> logger, IWebProxy webProxy) : base(logger, webProxy: webProxy) { }

        /// <remarks>
        /// Price list is updated every minute
        /// </remarks>
        /// <see cref="https://loot.farm/en/pricelist.html"/>
        /// <returns></returns>
        public async Task<IEnumerable<LootFarmItemPrice>> GetItemPricesAsync(string appName)
        {
            using (var client = BuildWebBrowserHttpClient(referrer: new Uri(BaseUri)))
            {
                if (String.Equals(appName, "CSGO", StringComparison.InvariantCultureIgnoreCase))
                {
                    appName = String.Empty; // CSGO is the app name default I guess, they don't name it explicitly like other apps...
                }

                var url = $"{BaseUri}fullprice{appName.ToUpper()}.json";
                var response = await RetryPolicy.ExecuteAsync(() => client.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                var responseJson = JsonSerializer.Deserialize<LootFarmItemPrice[]>(textJson);
                return responseJson;
            }
        }
    }
}
