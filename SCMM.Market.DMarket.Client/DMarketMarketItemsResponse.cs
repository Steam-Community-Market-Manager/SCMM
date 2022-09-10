using System.Text.Json.Serialization;

namespace SCMM.Market.DMarket.Client
{
    public class DMarketMarketItemsResponse
    {
        [JsonPropertyName("objects")]
        public IEnumerable<DMarketItem> Objects { get; set; }

        [JsonPropertyName("total")]
        public DMarketPaginationTotals Total { get; set; }

        [JsonPropertyName("cursor")]
        public string Cursor { get; set; }
    }
}
