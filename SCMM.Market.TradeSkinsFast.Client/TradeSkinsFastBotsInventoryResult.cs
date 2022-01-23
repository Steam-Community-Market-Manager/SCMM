using System.Text.Json.Serialization;

namespace SCMM.Market.TradeSkinsFast.Client
{
    public class TradeSkinsFastBotsInventoryResult
    {
        [JsonPropertyName("items")]
        public TradeSkinsFastItemListings Items { get; set; }
    }
}
