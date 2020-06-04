using Newtonsoft.Json;
using SCMM.Steam.Shared.Models;
using System.Collections.Generic;

namespace SCMM.Steam.Shared.Responses
{
    public class SteamInventoryPaginatedResponse : SteamResponse
    {
        [JsonProperty("total_inventory_count")]
        public int TotalInventoryCount { get; set; }

        [JsonProperty("assets")]
        public List<SteamInventoryAsset> Assets { get; set; }

        [JsonProperty("descriptions")]
        public List<SteamInventoryAssetDescription> Descriptions { get; set; }
    }
}
