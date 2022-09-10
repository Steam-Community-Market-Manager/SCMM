using System.Text.Json.Serialization;

namespace SCMM.Market.iTradegg.Client
{
    public class iTradeggItem
    {
        [JsonPropertyName("steamid")]
        public string SteamId { get; set; }

        [JsonPropertyName("ids")]
        public string[] Ids { get; set; }

        [JsonPropertyName("game")]
        public ulong Game { get; set; }

        [JsonPropertyName("assetid")]
        public string AssetId { get; set; }

        [JsonPropertyName("uniqID")]
        public string UniqueId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("same")]
        public int Same { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }
    }
}
