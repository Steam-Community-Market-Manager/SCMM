using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Workshop.Responses
{
    public class SteamWorkshopDownloaderStatusJsonResponse
    {
        [JsonPropertyName("age")]
        public uint Age { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("progress")]
        public uint Progress { get; set; }

        [JsonPropertyName("progressText")]
        public string ProgressText { get; set; }

        [JsonPropertyName("downloadError")]
        public string DownloadError { get; set; }
    }
}
