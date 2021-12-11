using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Workshop.Requests
{
    public class SteamWorkshopDownloaderJsonRequest
    {
        [JsonPropertyName("publishedFileId")]
        public ulong PublishedFileId { get; set; }

        [JsonPropertyName("collectionId")]
        public ulong? CollectionId { get; set; }

        [JsonPropertyName("hidden")]
        public bool Hidden { get; set; } = true;

        [JsonPropertyName("direct")]
        public bool direct { get; set; } = false;

        [JsonPropertyName("downloadFormat")]
        public string DownloadFormat { get; set; } = "raw";

        [JsonPropertyName("autodownload")]
        public bool AutoDownload { get; set; } = true;
    }
}
