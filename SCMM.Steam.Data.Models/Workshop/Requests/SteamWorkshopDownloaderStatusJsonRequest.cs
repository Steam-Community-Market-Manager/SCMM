using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Workshop.Requests
{
    public class SteamWorkshopDownloaderStatusJsonRequest
    {
        [JsonPropertyName("uuids")]
        public Guid[] Uuids { get; set; }
    }
}
