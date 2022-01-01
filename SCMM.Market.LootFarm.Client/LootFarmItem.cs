using System.Text.Json.Serialization;

namespace SCMM.Market.LootFarm.Client
{
    public class LootFarmItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("price")]
        public long Price { get; set; }

        [JsonPropertyName("have")]
        public int Have { get; set; }

        [JsonPropertyName("max")]
        public int Max { get; set; }

        [JsonPropertyName("rate")]
        public string Rate { get; set; }
    }
}
