using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Models
{
    public class PublishedFilePlaytimeStats
    {
        [JsonPropertyName("playtime_seconds")]
        public ulong PlaytimeSeconds { get; set; }

        [JsonPropertyName("num_sessions")]
        public ulong NumSessions { get; set; }
    }
}
