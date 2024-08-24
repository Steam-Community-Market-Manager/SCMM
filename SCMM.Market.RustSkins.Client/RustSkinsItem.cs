using System.Text.Json.Serialization;

namespace SCMM.Market.RustSkins.Client
{
    public class RustSkinsItem
    {
        [JsonPropertyName("item")]
        public string Item { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("price")]
        public float Price { get; set; }
    }
}
