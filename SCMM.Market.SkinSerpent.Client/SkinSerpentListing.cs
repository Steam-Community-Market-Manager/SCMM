using System.Text.Json.Serialization;

namespace SCMM.Market.SkinSerpent.Client
{
    public class SkinSerpentListing
    {
        [JsonPropertyName("appid")]
        public long AppId { get; set; }

        [JsonPropertyName("assetid")]
        public string AssetId { get; set; }

        [JsonPropertyName("skin")]
        public SkinSerpentSkin Skin { get; set; }

        [JsonPropertyName("price")]
        public int Price { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }
    }
}
