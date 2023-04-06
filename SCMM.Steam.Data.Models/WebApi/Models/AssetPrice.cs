using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Models
{
    public class AssetPrice
    {
        [JsonPropertyName("prices")]
        public Dictionary<string, long> Prices { get; set; }

        [JsonPropertyName("original_prices")]
        public Dictionary<string, long> OriginalPrices { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("class")]
        public AssetClass[] Class { get; set; }

        [JsonPropertyName("classid")]
        public string ClassId { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }
    }
}
