using Newtonsoft.Json;

namespace SCMM.Steam.Shared.Models
{
    public class SteamInventoryAsset
    {
        [JsonProperty("assetid")]
        public string AssetId { get; set; }

        [JsonProperty("classid")]
        public string ClassId { get; set; }

        [JsonProperty("amount")]
        public int Amount { get; set; }
    }
}
