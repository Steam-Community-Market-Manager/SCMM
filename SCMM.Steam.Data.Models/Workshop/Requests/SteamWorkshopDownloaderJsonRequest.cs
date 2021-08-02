using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.Workshop.Requests
{
    public class SteamWorkshopDownloaderJsonRequest
    {
        [JsonProperty("publishedFileId")]
        public ulong PublishedFileId { get; set; }

        [JsonProperty("collectionId")]
        public ulong? CollectionId { get; set; }

        [JsonProperty("extract")]
        public bool Extract { get; set; } = false;

        [JsonProperty("hidden")]
        public bool Hidden { get; set; } = true;

        [JsonProperty("direct")]
        public bool direct { get; set; } = false;

        [JsonProperty("autodownload")]
        public bool AutoDownload { get; set; } = true;
    }
}
