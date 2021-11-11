using SCMM.Steam.Data.Models.Community.Models;
using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Responses.Json
{
    public class SteamMarketSearchPaginatedJsonResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("start")]
        public int Start { get; set; }

        [JsonPropertyName("pagesize")]
        public int PageSize { get; set; }

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("results")]
        public List<SteamMarketSearchItem> Results { get; set; }
    }
}
