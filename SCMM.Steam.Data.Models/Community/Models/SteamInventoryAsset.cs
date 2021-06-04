using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamInventoryAsset
    {
        [JsonProperty("assetid")]
        public ulong AssetId { get; set; }

        [JsonProperty("classid")]
        public ulong ClassId { get; set; }

        [JsonProperty("amount")]
        public uint Amount { get; set; }
    }
}
