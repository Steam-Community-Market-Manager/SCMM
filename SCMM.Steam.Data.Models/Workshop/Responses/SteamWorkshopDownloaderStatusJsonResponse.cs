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

        [JsonPropertyName("bytes_size")]
        public ulong BytesSize { get; set; }

        [JsonPropertyName("bytes_transmitted")]
        public ulong BytesTransmitted { get; set; }

        [JsonPropertyName("storageNode")]
        public string StorageNode { get; set; }

        [JsonPropertyName("storagePath")]
        public string StoragePath { get; set; }
    }
}
