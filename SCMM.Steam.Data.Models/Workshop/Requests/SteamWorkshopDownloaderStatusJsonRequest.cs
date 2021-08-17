using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Workshop.Requests
{
    public class SteamWorkshopDownloaderStatusJsonRequest
    {
        [JsonProperty("uuids")]
        public Guid[] Uuids { get; set; }
    }
}
