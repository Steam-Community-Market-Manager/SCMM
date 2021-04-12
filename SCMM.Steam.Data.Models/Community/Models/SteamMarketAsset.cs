using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamMarketAsset : SteamAssetDescription
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("currency")]
        public int Currency { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("unowned_id")]
        public string UnownedId { get; set; }

        [JsonProperty("unowned_contextid")]
        public string UnownedContextId { get; set; }

        [JsonProperty("new_id")]
        public string NewId { get; set; }

        [JsonProperty("new_contextid")]
        public string NewContextId { get; set; }
    }
}
