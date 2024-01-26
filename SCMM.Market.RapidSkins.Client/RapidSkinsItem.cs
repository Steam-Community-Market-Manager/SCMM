using System.Text.Json.Serialization;

namespace SCMM.Market.RapidSkins.Client
{
    public class RapidSkinsItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("price")]
        public long Price { get; set; }
    }
}
