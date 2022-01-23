using SCMM.Market.Client;
using System.Text.Json;

namespace SCMM.Market.TradeSkinsFast.Client
{
    public class TradeSkinsFastWebClient
    {
        private const string BaseUri = "https://tradeskinsfast.com/";

        private HttpClient BuildMarketAPIClient()
        {
            // NOTE: Must supply this header else we get "invalid request"
            var client = new MarketHttpClient();
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            return client;

        }

        public async Task<TradeSkinsFastBotsInventoryResult> PostBotsInventoryAsync(string appId)
        {
            using (var client = BuildMarketAPIClient())
            {
                var url = $"{BaseUri}ajax/botsinventory";
                var payload = new FormUrlEncodedContent(new Dictionary<string, string>() {
                    { "appid", appId }
                });

                var response = await client.PostAsync(url, payload);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<TradeSkinsFastResponse<TradeSkinsFastBotsInventoryResult>>(textJson);
                return responseJson?.Response;
            }
        }
    }
}
