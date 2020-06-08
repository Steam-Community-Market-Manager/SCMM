using Newtonsoft.Json;
using SCMM.Steam.Shared.Models;

namespace SCMM.Steam.Shared.Responses.Community.Json
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
