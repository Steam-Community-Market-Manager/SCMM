using System.Text.Json.Serialization;

namespace SCMM.Market.SnipeSkins.Client
{
    public class SnipeSkinItem
    {
        [JsonPropertyName("marketHashName")]
        public string MarketHashName { get; set; }

        [JsonPropertyName("lowestMarketPrice")]
        public long LowestMarketPrice { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

    }
}
