using System.Text.Json.Serialization;

namespace SCMM.Market.CSDeals.Client
{
    public class CSDealsMarketplaceSearchResults<T>
    {
        [JsonPropertyName("totalResults")]
        public long TotalResults { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("perPage")]
        public int PerPage { get; set; }

        [JsonPropertyName("results")]
        public T Results { get; set; }
    }
}
