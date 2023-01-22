using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Store.Responses.Json
{
    public class SteamItemStoreGetItemDefsPaginatedJsonResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("pagesize")]
        public int PageSize { get; set; }

        [JsonPropertyName("start")]
        public int Start { get; set; }

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("results_html")]
        public string ResultsHtml { get; set; }
    }
}
