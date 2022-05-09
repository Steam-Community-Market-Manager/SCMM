using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamInventoryAsset
    {
        [JsonPropertyName("appid")]
        public ulong AppId { get; set; }

        [JsonPropertyName("contextid")]
        public ulong ContextId { get; set; }

        [JsonPropertyName("assetid")]
        public ulong AssetId { get; set; }

        [JsonPropertyName("classid")]
        public ulong ClassId { get; set; }

        [JsonPropertyName("instanceid")]
        public ulong InstanceId { get; set; }

        [JsonPropertyName("amount")]
        public uint Amount { get; set; }
    }
}
