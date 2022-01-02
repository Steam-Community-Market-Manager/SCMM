using System.Text.Json.Serialization;

namespace SCMM.Market.TradeitGG.Client
{
    public class TradeitGGInventoryDataResponse
    {
        [JsonPropertyName("items")]
        public IEnumerable<TradeitGGItem> Items { get; set; }

        [JsonPropertyName("counts")]
        public IDictionary<string, int> Counts { get; set; }
    }
}
