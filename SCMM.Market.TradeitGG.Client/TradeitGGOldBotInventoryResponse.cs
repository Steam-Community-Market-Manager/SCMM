using System.Text.Json.Serialization;

namespace SCMM.Market.TradeitGG.Client
{
    public class TradeitGGOldBotInventoryResponse
    {
        public const string ItemsKey = "items";

        [JsonPropertyName("steamid")]
        public string SteamId { get; set; }

        [JsonPropertyName("730")]
        public IDictionary<string, IEnumerable<KeyValuePair<string, TradeitGGOldBotInventoryItem>>> CSGOItems { get; set; }

        [JsonPropertyName("252490")]
        public IDictionary<string, IEnumerable<KeyValuePair<string, TradeitGGOldBotInventoryItem>>> RustItems { get; set; }

        public IEnumerable<KeyValuePair<string, TradeitGGOldBotInventoryItem>> Items
        {
            get
            {
                var result = new List<KeyValuePair<string, TradeitGGOldBotInventoryItem>>();
                result.AddRange(CSGOItems.FirstOrDefault(x => x.Key == ItemsKey).Value ?? new List<KeyValuePair<string, TradeitGGOldBotInventoryItem>>());
                result.AddRange(RustItems.FirstOrDefault(x => x.Key == ItemsKey).Value ?? new List<KeyValuePair<string, TradeitGGOldBotInventoryItem>>());
                return result;
            }
        }
    }
}
