using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SCMM.Market.TradeitGG.Client
{
    public class TradeitGGWebClient : Shared.Client.WebClient
    {
        private const string BaseUri = "https://tradeit.gg/api/v2/";

        public const int MaxPageLimit = 200;

        public TradeitGGWebClient(IWebProxy webProxy) : base(webProxy: webProxy) { }

        public async Task<IDictionary<TradeitGGItem, int>> GetInventoryDataAsync(string appId, int offset = 0, int limit = MaxPageLimit)
        {
            using (var client = BuildWebBrowserHttpClient())
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

        public async Task<IEnumerable<string>> GetBotAccountsAsync()
        {
            using (var client = BuildWebBrowserHttpClient())
            {
                var bots = new List<string>();
                var pageUrls = new[]
                {
                    "https://steamcommunity.com/groups/tradeitggbots/members/?p=1&content_only=true",
                    "https://steamcommunity.com/groups/tradeitggbots/members/?p=2&content_only=true",
                    "https://steamcommunity.com/groups/tradeitggbots/members/?p=3&content_only=true",
                    "https://steamcommunity.com/groups/tradeitggbots/members/?p=4&content_only=true"
                };

                foreach (var pageUrl in pageUrls)
                {
                    var response = await client.GetAsync(pageUrl);
                    response.EnsureSuccessStatusCode();

                    var text = await response.Content.ReadAsStringAsync();
                    var profiles = Regex.Matches(text, @"\/profiles\/([0-9]+)");
                    foreach (var profile in profiles.OfType<Match>())
                    {
                        bots.Add(profile.Groups[1].Value);
                    }
                }

                return bots.Distinct();
            }
        }
    }
}
