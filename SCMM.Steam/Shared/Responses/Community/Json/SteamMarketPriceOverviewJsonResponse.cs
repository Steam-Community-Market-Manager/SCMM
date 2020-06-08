using Newtonsoft.Json;

namespace SCMM.Steam.Shared.Responses.Community.Json
{
    public class SteamMarketPriceOverviewJsonResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("lowest_price")]
        public string LowestPrice { get; set; }

        [JsonProperty("volume")]
        public string Volume { get; set; }

        [JsonProperty("median_price")]
        public string MedianPrice { get; set; }
    }
}