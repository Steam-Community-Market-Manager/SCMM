using System.Text.Json.Serialization;

namespace SCMM.Market.SkinSerpent.Client
{
    public class SkinSerpentListingsResponse
    {
        [JsonPropertyName("listings")]
        public SkinSerpentListing[] Listings { get; set; }

        [JsonPropertyName("currentPage")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("prevPage")]
        public string PrevPage { get; set; }

        [JsonPropertyName("nextPage")]
        public string NextPage { get; set; }
    }
}
