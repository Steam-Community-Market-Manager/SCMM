using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.WebApi.Models
{
    public class PublishedFilePreview
    {
        [JsonProperty("previewid")]
        public string PreviewId { get; set; }

        [JsonProperty("preview_type")]
        public uint PreviewType { get; set; }

        [JsonProperty("youtubevideoid")]
        public string YouTubeVideoId { get; set; }

        [JsonProperty("external_reference")]
        public string ExternalReference { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("size")]
        public ulong Size { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("sortorder")]
        public uint SortOrder { get; set; }
    }
}
