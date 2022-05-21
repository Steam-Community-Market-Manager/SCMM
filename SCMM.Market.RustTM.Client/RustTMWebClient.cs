using SCMM.Worker.Client;
using SCMM.Steam.Data.Models;
using System.Text.Json;

namespace SCMM.Market.RustTM.Client
{
    public class RustTMWebClient : Worker.Client.WebClient
    {
        private const string BaseUri = "https://rust.tm/api/v2/";

        public async Task<IEnumerable<RustTMItem>> GetPricesAsync(string currencyName = Constants.SteamCurrencyUSD)
        {
            using (var client = BuildHttpClient())
            {
                var url = $"{BaseUri}prices/{currencyName.ToUpper()}.json";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<RustTMPricesResponse>(textJson);
                return responseJson?.Items;
            }
        }
    }
}
