using System.Text.Json.Serialization;

namespace SCMM.Market.DMarket.Client
{
    public class DMarketPaginationTotals
    {
        [JsonPropertyName("offers")]
        public int Offers { get; set; }

        [JsonPropertyName("targets")]
        public int Targets { get; set; }

        [JsonPropertyName("items")]
        public long Items { get; set; }

        [JsonPropertyName("completedOffers")]
        public string CompletedOffers { get; set; }

        [JsonPropertyName("closedTargets")]
        public string ClosedTargets { get; set; }
    }
}
