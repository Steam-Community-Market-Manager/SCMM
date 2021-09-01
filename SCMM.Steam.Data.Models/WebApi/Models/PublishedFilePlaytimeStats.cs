using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.WebApi.Models
{
    public class PublishedFilePlaytimeStats
    {
        [JsonProperty("playtime_seconds")]
        public ulong PlaytimeSeconds { get; set; }

        [JsonProperty("num_sessions")]
        public ulong NumSessions { get; set; }
    }
}
