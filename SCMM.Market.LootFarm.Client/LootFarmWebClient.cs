using SCMM.Market.Client;
using System.Text.Json;

namespace SCMM.Market.LootFarm.Client
{
    public class LootFarmWebClient : AgentWebClient
    {
        private const string BaseUri = "https://loot.farm/";

        public async Task<IEnumerable<LootFarmItemPrice>> GetItemPricesAsync(string appName)
        {
            using (var client = BuildHttpClient())
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
