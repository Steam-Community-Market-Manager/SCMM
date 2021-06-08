using Newtonsoft.Json;
using SCMM.Steam.Data.Models.Community.Models;
using System.Collections.Generic;

namespace SCMM.Steam.Data.Models.Community.Responses.Json
{
    public class SteamMarketMyListingsPaginatedJsonResponse
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

        [JsonProperty("listings")]
        public List<SteamMarketListing> Listings { get; set; }

        [JsonProperty("listings_on_hold")]
        public List<SteamMarketListing> ListingsOnHold { get; set; }

        [JsonProperty("listings_to_confirm")]
        public List<SteamMarketListing> ListingsToConfirm { get; set; }

        [JsonProperty("buy_orders")]
        public List<SteamMarketBuyOrder> BuyOrders { get; set; }
    }
}
