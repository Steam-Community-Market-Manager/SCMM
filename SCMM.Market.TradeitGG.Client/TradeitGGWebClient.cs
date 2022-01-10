using System.Net.Http.Headers;
using System.Text.Json;

namespace SCMM.Market.TradeitGG.Client
{
    public class TradeitGGWebClient
    {
        public const decimal StoreDiscountMultiplier = 0.25m; // 25% off

        private const string BaseUri = "https://tradeit.gg/api/v2/";

        private HttpClient WebBrowserLikeClient()
        {
            // NOTE: We need to pretend we are a web browser as tradeit.gg uses a CloudFlare firewall with some rules that block clients that don't look like typical web browsers.
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(Windows NT 10.0; Win64; x64)"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AppleWebKit", "537.36"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(KHTML, like Gecko)"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Chrome", "96.0.4664.110"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Safari", "537.36"));
            return client;

        }
        public async Task<IDictionary<TradeitGGItem, int>> GetInventoryDataAsync(string appId, int offset = 0, int limit = 200)
        {
            using (var client = WebBrowserLikeClient())
            {
                try
                {
                    var url = $"{BaseUri}inventory/data?gameId={Uri.EscapeDataString(appId)}&offset={offset}&limit={limit}&fresh=true";
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var textJson = await response.Content.ReadAsStringAsync();
                    var responseJson = JsonSerializer.Deserialize<TradeitGGInventoryDataResponse>(textJson);
                    var inventoryDataJoined = responseJson?.Items?.Join(responseJson.Counts ?? new Dictionary<string, int>(),
                        x => x.GroupId.ToString(),
                        x => x.Key,
                        (item, count) => new
                        {
                            Item = item,
                            Count = count
                        }
                    );

                    return inventoryDataJoined?.ToDictionary(
                        x => x.Item,
                        x => x.Count.Value
                    );
                }
                catch (JsonException)
                {
                    // TODO: If "counts" is empty, we get an empty array in the JSON, which cannot be deserialised as a dictionary.
                    //       Need to add a customer type converter to handle this better but I cbf writing one...
                    return null;
                }
            }
        }
    }
}
