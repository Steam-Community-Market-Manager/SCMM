using System.Text.Json.Serialization;

namespace SCMM.Market.SwapGG.Client
{
    public class SwapGGMarketItem
    {
        [JsonPropertyName("price")]
        public long Price { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }
    }
}
