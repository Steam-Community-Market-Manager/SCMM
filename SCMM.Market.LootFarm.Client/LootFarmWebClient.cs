using System.Net;
using System.Text.Json;

namespace SCMM.Market.LootFarm.Client
{
    public class LootFarmWebClient : Shared.Web.Client.WebClient
    {
        private const string BaseUri = "https://loot.farm/";

        public LootFarmWebClient(IWebProxy webProxy) : base(webProxy: webProxy) { }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Updated every minute
        /// </remarks>
        /// <see cref="https://loot.farm/en/pricelist.html"/>
        /// <param name="appName"></param>
        /// <returns></returns>
        public async Task<IEnumerable<LootFarmItemPrice>> GetItemPricesAsync(string appName)
        {
            using (var client = BuildWebBrowserHttpClient(referrer: new Uri(BaseUri)))
            {
                if (String.Equals(appName, "CSGO", StringComparison.InvariantCultureIgnoreCase))
                {
                    appName = String.Empty; // CSGO is considered the default I guess, don't name it explicitly...
                }

                var url = $"{BaseUri}fullprice{appName.ToUpper()}.json";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<LootFarmItemPrice[]>(textJson);
                return responseJson;
            }
        }
    }
}
