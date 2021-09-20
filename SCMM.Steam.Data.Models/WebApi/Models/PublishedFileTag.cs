using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Models
{
    public class PublishedFileTag
    {
        [JsonPropertyName("tag")]
        public string Tag { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }
    }
}
