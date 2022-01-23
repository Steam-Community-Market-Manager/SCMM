using System.Text.Json.Serialization;

namespace SCMM.Market.SkinsMonkey.Client
{
    public class SkinsMonkeyItem
    {
        [JsonPropertyName("appId")]
        public ulong AppId { get; set; }

        [JsonPropertyName("marketName")]
        public string MarketName { get; set; }

        [JsonPropertyName("price")]
        public long Price { get; set; }
    }
}
