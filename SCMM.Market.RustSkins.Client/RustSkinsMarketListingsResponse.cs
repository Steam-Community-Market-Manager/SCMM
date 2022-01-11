using System.Text.Json.Serialization;

namespace SCMM.Market.RustSkins.Client
{
    public class RustSkinsMarketListingsResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("max")]
        public int Max { get; set; }

        [JsonPropertyName("aggregatedMetaOffers")]
        public IEnumerable<RustSkinsItemListing> Listings { get; set; }
    }
}
