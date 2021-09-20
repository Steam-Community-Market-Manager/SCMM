using System.Text.Json.Serialization;
using SCMM.Steam.Data.Models.Community.Models;

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
