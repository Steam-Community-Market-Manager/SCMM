using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SCMM.Market.TradeitGG.Client
{
    public class TradeitGGWebClient : Shared.Client.WebClient
    {
        private const string InventoryBaseUri = "https://inventory.tradeit.gg/";
        private const string OldWebsiteBaseUri = "https://old.tradeit.gg/";
        private const string WebsiteBaseUri = "https://tradeit.gg/";
        private const string ApiBaseUri = "https://tradeit.gg/api/v2/";

        public const int MaxPageLimit = 1000;

        public TradeitGGWebClient(IWebProxy webProxy) : base(webProxy: webProxy) { }

        /// <summary>
        /// 
        /// </summary>
        /// <see cref="https://old.tradeit.gg/?back-to-old=true"/>
        /// <param name="appId"></param>
        /// <returns></returns>
        [Obsolete("This API might stop working at any point, use GetNewInventoryDataAsync() instead")]
        public async Task<IEnumerable<TradeitGGItem>> GetOldInventoryAsync(string appId)
        {
            using (var client = BuildWebBrowserHttpClient(referer: new Uri(OldWebsiteBaseUri)))
            {
                var url = $"{InventoryBaseUri}sinv/{Uri.EscapeDataString(appId)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<IEnumerable<TradeitGGOldBotInventoryResponse>>(textJson);
                var inventoryData = responseJson?.SelectMany(x =>
                    x.Items.Select(i => new TradeitGGItem()
                    {
                        GroupId = Int64.Parse(i.Key.Split("_", StringSplitOptions.TrimEntries).FirstOrDefault()),
                        Name = i.Key.Split("_", StringSplitOptions.TrimEntries).FirstOrDefault(),
                        Price = i.Value.Price
                    })
                );

                return inventoryData;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="offset"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public async Task<IDictionary<TradeitGGItem, int>> GetNewInventoryDataAsync(string appId, int offset = 0, int limit = MaxPageLimit)
        {
            using (var client = BuildWebBrowserHttpClient(referer: new Uri(WebsiteBaseUri)))
            {
                try
                {
                    var url = $"{ApiBaseUri}inventory/data?gameId={Uri.EscapeDataString(appId)}&sortType=Popularity&offset={offset}&limit={limit}&fresh=true";
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
