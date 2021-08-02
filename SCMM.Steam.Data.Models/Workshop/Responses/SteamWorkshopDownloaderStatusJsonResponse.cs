using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Workshop.Responses
{
    public class SteamWorkshopDownloaderStatusJsonResponse
    {
        [JsonProperty("age")]
        public uint Age { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("progress")]
        public uint Progress { get; set; }

        [JsonProperty("progressText")]
        public string ProgressText { get; set; }

        [JsonProperty("downloadError")]
        public string DownloadError { get; set; }
    }
}
