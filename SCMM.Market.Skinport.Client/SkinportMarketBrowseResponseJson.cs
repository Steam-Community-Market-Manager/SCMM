using System.Text.Json.Serialization;

namespace SCMM.Market.Skinport.Client
{
    public class SkinportMarketBrowseResponseJson
    {
        [JsonPropertyName("requestId")]
        public Guid RequestId { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("items")]
        public IEnumerable<SkinportMarketItem> Items { get; set; }
    }
}
