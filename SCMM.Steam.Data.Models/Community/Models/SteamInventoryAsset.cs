using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Community.Models
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
