using Newtonsoft.Json;
using SCMM.Steam.Data.Models.Community.Models;

namespace SCMM.Steam.Data.Models.Community.Responses.Json
{
    public class SteamMarketMyHistoryPaginatedJsonResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("start")]
        public int Start { get; set; }

        [JsonProperty("pagesize")]
        public int PageSize { get; set; }

        [JsonProperty("total_count")]
        public int TotalCount { get; set; }

        /// <summary>
        /// n0: AppId
        /// n1: ContextId
        /// n2: AssetId
        /// </summary>
        [JsonProperty("assets")]
        public Dictionary<string, Dictionary<string, Dictionary<string, SteamAssetClass>>> Assets { get; set; }

        [JsonProperty("events")]
        public List<SteamMarketEvent> Events { get; set; }

        [JsonProperty("purchases")]
        public Dictionary<string, SteamMarketPurchase> Purchases { get; set; }

        [JsonProperty("listings")]
        public Dictionary<string, SteamMarketListing> Listings { get; set; }
    }
}
