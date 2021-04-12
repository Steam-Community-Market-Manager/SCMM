using Newtonsoft.Json;
using SCMM.Steam.Data.Models.Community.Models;

namespace SCMM.Steam.Data.Models.Community.Responses.Json
{
    public class SteamMarketItemOrdersActivityJsonResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("activity")]
        public SteamMarketItemActivity[] Activity { get; set; }

    }
}
