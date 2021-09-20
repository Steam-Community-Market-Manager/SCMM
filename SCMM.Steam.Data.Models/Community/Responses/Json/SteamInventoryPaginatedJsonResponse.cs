using System.Text.Json.Serialization;
using SCMM.Steam.Data.Models.Community.Models;

namespace SCMM.Steam.Data.Models.Community.Responses.Json
{
    public class SteamInventoryPaginatedJsonResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("total_inventory_count")]
        public int TotalInventoryCount { get; set; }

        [JsonPropertyName("assets")]
        public List<SteamInventoryAsset> Assets { get; set; }

        [JsonPropertyName("descriptions")]
        public List<SteamAssetClass> Descriptions { get; set; }
    }
}
