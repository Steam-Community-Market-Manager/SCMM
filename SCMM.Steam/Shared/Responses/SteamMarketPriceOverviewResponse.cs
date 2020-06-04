using Newtonsoft.Json;

namespace SCMM.Steam.Shared.Responses
{
    public class SteamMarketPriceOverviewResponse : SteamResponse
    {
        [JsonProperty("lowest_price")]
        public string LowestPrice { get; set; }

        [JsonProperty("volume")]
        public string Volume { get; set; }

        [JsonProperty("median_price")]
        public string MedianPrice { get; set; }
    }
}