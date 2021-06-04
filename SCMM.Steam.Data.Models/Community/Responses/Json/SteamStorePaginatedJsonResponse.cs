using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Community.Responses.Json
{
    public class SteamStorePaginatedJsonResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("pagesize")]
        public int PageSize { get; set; }

        [JsonProperty("start")]
        public int Start { get; set; }

        [JsonProperty("total_count")]
        public int TotalCount { get; set; }

        [JsonProperty("results_html")]
        public string ResultsHtml { get; set; }
    }
}
