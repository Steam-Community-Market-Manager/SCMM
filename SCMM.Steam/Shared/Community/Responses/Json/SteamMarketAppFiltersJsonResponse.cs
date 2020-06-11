using Newtonsoft.Json;
using SCMM.Steam.Shared.Community.Models;
using System.Collections.Generic;

namespace SCMM.Steam.Shared.Community.Responses.Json
{
    public class SteamMarketAppFiltersJsonResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("facets")]
        public Dictionary<string, SteamAssetFilter> Facets { get; set; }
    }
}
