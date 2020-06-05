using Newtonsoft.Json;
using SCMM.Steam.Shared.Models;
using System.Collections.Generic;

namespace SCMM.Steam.Shared.Responses
{
    public class SteamMarketHistoryPaginatedResponse : SteamResponse
    {
        [JsonProperty("start")]
        public int Start { get; set; }

        [JsonProperty("pagesize")]
        public int PageSize { get; set; }

        [JsonProperty("total_count")]
        public int TotalCount { get; set; }

        [JsonProperty("assets")]
        public Dictionary<string, Dictionary<string, Dictionary<string, SteamMarketHistoryAssetDescription>>> Assets { get; set; }

        [JsonProperty("events")]
        public List<SteamMarketHistoryEvent> Events { get; set; }

        [JsonProperty("purchases")]
        public Dictionary<string, SteamMarketHistoryPurchase> Purchases { get; set; }

        [JsonProperty("listings")]
        public Dictionary<string, SteamMarketHistoryListing> Listings { get; set; }
    }
}
