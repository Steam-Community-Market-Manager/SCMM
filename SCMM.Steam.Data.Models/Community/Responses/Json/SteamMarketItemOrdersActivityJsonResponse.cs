using SCMM.Steam.Data.Models.Community.Models;
using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Responses.Json
{
    public class SteamMarketItemOrdersActivityJsonResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("activity")]
        public SteamMarketItemActivity[] Activity { get; set; }

    }
}
