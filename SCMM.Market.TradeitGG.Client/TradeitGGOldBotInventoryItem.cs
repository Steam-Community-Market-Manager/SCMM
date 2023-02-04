using System.Text.Json.Serialization;

namespace SCMM.Market.TradeitGG.Client
{
    public class TradeitGGOldBotInventoryItem
    {
        [JsonPropertyName("p")]
        public long Price { get; set; }
    }
}
