using Newtonsoft.Json;
using SCMM.Steam.Data.Models.Community.Models;
using System.Collections.Generic;

namespace SCMM.Steam.Data.Models.Community.Responses.Json
{
    public class SteamMarketAppFiltersJsonResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("facets")]
        public Dictionary<string, SteamAppFilter> Facets { get; set; }
    }
}
