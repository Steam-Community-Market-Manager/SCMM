using System.Text.Json.Serialization;

namespace SCMM.Market.ManncoStore.Client
{
    public class ManncoStoreItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("nbb")]
        public int Count { get; set; }
    }
}
