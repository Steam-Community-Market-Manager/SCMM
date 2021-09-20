using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Models
{
    public class PublishedFilePreview
    {
        [JsonPropertyName("previewid")]
        public string PreviewId { get; set; }

        [JsonPropertyName("preview_type")]
        public uint PreviewType { get; set; }

        [JsonPropertyName("youtubevideoid")]
        public string YouTubeVideoId { get; set; }

        [JsonPropertyName("external_reference")]
        public string ExternalReference { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("size")]
        public ulong Size { get; set; }

        [JsonPropertyName("filename")]
        public string Filename { get; set; }

        [JsonPropertyName("sortorder")]
        public uint SortOrder { get; set; }
    }
}
