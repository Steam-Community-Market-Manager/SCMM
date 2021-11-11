using SCMM.Steam.Data.Models.Community.Models;
using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Responses.Json
{
    public class SteamMarketMyListingsPaginatedJsonResponse
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

        [JsonPropertyName("listings")]
        public List<SteamMarketListing> Listings { get; set; }

        [JsonPropertyName("listings_on_hold")]
        public List<SteamMarketListing> ListingsOnHold { get; set; }

        [JsonPropertyName("listings_to_confirm")]
        public List<SteamMarketListing> ListingsToConfirm { get; set; }

        [JsonPropertyName("buy_orders")]
        public List<SteamMarketBuyOrder> BuyOrders { get; set; }
    }
}
