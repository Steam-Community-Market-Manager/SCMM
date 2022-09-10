using System.Text.Json.Serialization;

namespace SCMM.Market.CSDeals.Client
{
    public class CSDealsItemPrice
    {
        [JsonPropertyName("marketname")]
        public string MarketName { get; set; }

        [JsonPropertyName("lowest_price")]
        public string LowestPrice { get; set; }
    }
}
