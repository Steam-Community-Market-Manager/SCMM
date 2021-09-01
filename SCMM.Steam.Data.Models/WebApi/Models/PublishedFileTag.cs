using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.WebApi.Models
{
    public class PublishedFileTag
    {
        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }
    }
}
