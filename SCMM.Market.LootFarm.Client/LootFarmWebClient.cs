using System.Text.Json;

namespace SCMM.Market.LootFarm.Client
{
    public class LootFarmWebClient
    {
        private const string BaseUri = "https://loot.farm/";

        public async Task<IEnumerable<LootFarmItemPrice>> GetItemPricesAsync(string appName)
        {
            using (var client = new HttpClient())
            {
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
