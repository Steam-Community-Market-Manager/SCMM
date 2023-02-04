using SCMM.Steam.Data.Models;
using System.Net;
using System.Text.Json;

namespace SCMM.Market.RustTM.Client
{
    public class RustTMWebClient : Shared.Client.WebClient
    {
        private const string ApiBaseUri = "https://rust.tm/api/v2/";

        public RustTMWebClient(IWebProxy webProxy) : base(webProxy: webProxy) { }

        public async Task<IEnumerable<RustTMItem>> GetPricesAsync(string currencyName = Constants.SteamCurrencyUSD)
        {
            using (var client = BuildWebApiHttpClient())
            {
                var url = $"{ApiBaseUri}prices/{currencyName.ToUpper()}.json";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<RustTMPricesResponse>(textJson);
                return responseJson?.Items;
            }
        }
    }
}
