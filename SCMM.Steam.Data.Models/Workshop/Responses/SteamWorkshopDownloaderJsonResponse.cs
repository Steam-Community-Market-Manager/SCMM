using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Workshop.Responses
{
    public class SteamWorkshopDownloaderJsonResponse
    {
        [JsonPropertyName("uuid")]
        public Guid Uuid { get; set; }
    }
}
