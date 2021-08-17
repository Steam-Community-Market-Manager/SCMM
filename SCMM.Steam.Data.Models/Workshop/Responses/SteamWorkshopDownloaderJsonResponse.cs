using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Workshop.Responses
{
    public class SteamWorkshopDownloaderJsonResponse
    {
        [JsonProperty("uuid")]
        public Guid Uuid { get; set; }
    }
}
