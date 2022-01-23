using SCMM.Market.Client;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SCMM.Market.TradeitGG.Client
{
    public class TradeitGGWebClient
    {
        private const string BaseUri = "https://tradeit.gg/api/v2/";

        public async Task<IDictionary<TradeitGGItem, int>> GetInventoryDataAsync(string appId, int offset = 0, int limit = 200)
        {
            using (var client = new MarketHttpClient())
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
