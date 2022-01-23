using System.Text.Json;

namespace SCMM.Market.RustTM.Client
{
    public class RustTMWebClient
    {
        private const string BaseUri = "https://rust.tm/api/v2/";

        public async Task<IEnumerable<RustTMItem>> GetPricesAsync(string currencyName = "USD")
        {
            using (var client = new HttpClient())
            {
                var url = $"{BaseUri}prices/{currencyName.ToUpper()}.json";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<RustTMInventoryDataResponse>(textJson);
                return responseJson?.Items;
            }
        }
    }
}
