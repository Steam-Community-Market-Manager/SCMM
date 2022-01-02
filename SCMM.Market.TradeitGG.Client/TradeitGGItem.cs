using System.Text.Json.Serialization;

namespace SCMM.Market.TradeitGG.Client
{
    public class TradeitGGItem
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("groupId")]
        public long GroupId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("price")]
        public long Price { get; set; }
    }
}
