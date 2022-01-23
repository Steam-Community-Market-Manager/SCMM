using System.Text.Json.Serialization;

namespace SCMM.Market.TradeSkinsFast.Client
{
    public class TradeSkinsFastItemListing
    {
        [JsonPropertyName("c")]
        public string MarketName { get; set; }

        [JsonPropertyName("x")]
        public float MarketPrice { get; set; }

        [JsonPropertyName("i")]
        public float ListingPrice { get; set; }

        [JsonPropertyName("v")]
        public TradeSkinsFastItemId[] ItemIds { get; set; }

        [JsonPropertyName("w")]
        public bool IsUserListing { get; set; }
    }
}
