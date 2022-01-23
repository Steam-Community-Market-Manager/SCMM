using System.Text.Json.Serialization;

namespace SCMM.Market.SkinsMonkey.Client
{
    public class SkinsMonkeyItemListing
    {
        [JsonPropertyName("item")]
        public SkinsMonkeyItem Item { get; set; }

        [JsonPropertyName("appId")]
        public ulong AppId { get; set; }

        [JsonPropertyName("assetId")]
        public string AssetId { get; set; }

        [JsonPropertyName("uniqueId")]
        public string UniqueId { get; set; }

        [JsonPropertyName("tradeLockTime")]
        public long TradeLockTime { get; set; }

        [JsonPropertyName("tradeLock")]
        public bool TradeLock { get; set; }
    }
}
