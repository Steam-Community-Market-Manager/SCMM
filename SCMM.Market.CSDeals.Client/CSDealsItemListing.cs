using System.Text.Json.Serialization;

namespace SCMM.Market.CSDeals.Client
{
    public class CSDealsItemListing
    {
        [JsonPropertyName("c")]
        public string MarketName { get; set; }

        [JsonPropertyName("x")]
        public float MarketPrice { get; set; }

        [JsonPropertyName("i")]
        public float ListingPrice { get; set; }

        [JsonPropertyName("v")]
        public CSDealsItemId[] ItemIds { get; set; }
    }
}
