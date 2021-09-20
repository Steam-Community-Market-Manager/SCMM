using System.Text.Json.Serialization;
using SCMM.Steam.Data.Models.Community.Models;

namespace SCMM.Steam.Data.Models.Community.Responses.Json
{
    public class SteamMarketMyHistoryPaginatedJsonResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("start")]
        public int Start { get; set; }

        [JsonPropertyName("pagesize")]
        public int PageSize { get; set; }

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        /// <summary>
        /// n0: AppId
        /// n1: ContextId
        /// n2: AssetId
        /// </summary>
        [JsonPropertyName("assets")]
        public Dictionary<string, Dictionary<string, Dictionary<string, SteamAssetClass>>> Assets { get; set; }

        [JsonPropertyName("events")]
        public List<SteamMarketEvent> Events { get; set; }

        [JsonPropertyName("purchases")]
        public Dictionary<string, SteamMarketPurchase> Purchases { get; set; }

        [JsonPropertyName("listings")]
        public Dictionary<string, SteamMarketListing> Listings { get; set; }
    }
}
