using System.Text.Json.Serialization;

namespace SCMM.Market.SkinBaron.Client
{
    public class SkinBaronFilterOffersResponse
    {
        [JsonPropertyName("aggregatedMetaOffers")]
        public IEnumerable<SkinBaronItemOffer> AggregatedMetaOffers { get; set; }

        [JsonPropertyName("itemsPerPage")]
        public int ItemsPerPage { get; set; }

        [JsonPropertyName("numberOfOffers")]
        public int NumberOfOffers { get; set; }

        [JsonPropertyName("numberOfPages")]
        public int NumberOfPages { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }
    }
}
