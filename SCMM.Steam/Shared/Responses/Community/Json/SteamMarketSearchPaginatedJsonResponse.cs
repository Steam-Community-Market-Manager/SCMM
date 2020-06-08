using Newtonsoft.Json;
using SCMM.Steam.Shared.Models;
using System.Collections.Generic;

namespace SCMM.Steam.Shared.Responses.Community.Json
{
    public class SteamMarketSearchPaginatedJsonResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("start")]
        public int Start { get; set; }

        [JsonProperty("pagesize")]
        public int PageSize { get; set; }

        [JsonProperty("total_count")]
        public int TotalCount { get; set; }

        [JsonProperty("results")]
        public List<SteamMarketSearchItem> Results { get; set; }
    }
}
