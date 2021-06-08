using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamMarketAsset : SteamAssetClass
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }

        [JsonProperty("amount")]
        public uint Amount { get; set; }

        [JsonProperty("currency")]
        public uint Currency { get; set; }

        [JsonProperty("status")]
        public uint Status { get; set; }

        [JsonProperty("unowned_id")]
        public ulong UnownedId { get; set; }

        [JsonProperty("unowned_contextid")]
        public ulong UnownedContextId { get; set; }

        [JsonProperty("new_id")]
        public ulong NewId { get; set; }

        [JsonProperty("new_contextid")]
        public ulong NewContextId { get; set; }
    }
}
