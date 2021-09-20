using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamMarketAsset : SteamAssetClass
    {
        [JsonPropertyName("id")]
        public ulong Id { get; set; }

        [JsonPropertyName("amount")]
        public uint Amount { get; set; }

        [JsonPropertyName("currency")]
        public uint Currency { get; set; }

        [JsonPropertyName("status")]
        public uint Status { get; set; }

        [JsonPropertyName("unowned_id")]
        public ulong UnownedId { get; set; }

        [JsonPropertyName("unowned_contextid")]
        public ulong UnownedContextId { get; set; }

        [JsonPropertyName("new_id")]
        public ulong NewId { get; set; }

        [JsonPropertyName("new_contextid")]
        public ulong NewContextId { get; set; }
    }
}
