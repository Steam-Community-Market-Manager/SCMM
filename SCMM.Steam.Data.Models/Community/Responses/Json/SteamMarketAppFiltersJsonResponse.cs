using SCMM.Steam.Data.Models.Community.Models;
using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Responses.Json
{
    public class SteamMarketAppFiltersJsonResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("facets")]
        public Dictionary<string, SteamAppFilter> Facets { get; set; }
    }
}
