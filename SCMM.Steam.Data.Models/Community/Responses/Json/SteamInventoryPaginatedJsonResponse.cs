using Newtonsoft.Json;
using SCMM.Steam.Data.Models.Community.Models;
using System.Collections.Generic;

namespace SCMM.Steam.Data.Models.Community.Responses.Json
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
        public List<SteamAssetClass> Descriptions { get; set; }
    }
}
