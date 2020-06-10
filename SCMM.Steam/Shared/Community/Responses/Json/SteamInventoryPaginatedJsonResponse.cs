using Newtonsoft.Json;
using SCMM.Steam.Shared.Models;
using System.Collections.Generic;

namespace SCMM.Steam.Shared.Community.Responses.Json
{
    public class SteamInventoryPaginatedJsonResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("total_inventory_count")]
        public int TotalInventoryCount { get; set; }

        [JsonProperty("assets")]
        public List<SteamInventoryAsset> Assets { get; set; }

        [JsonProperty("descriptions")]
        public List<SteamAssetDescription> Descriptions { get; set; }
    }
}
