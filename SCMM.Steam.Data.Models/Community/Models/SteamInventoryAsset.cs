using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamInventoryAsset
    {
        [JsonPropertyName("assetid")]
        public ulong AssetId { get; set; }

        [JsonPropertyName("classid")]
        public ulong ClassId { get; set; }

        [JsonPropertyName("amount")]
        public uint Amount { get; set; }
    }
}
