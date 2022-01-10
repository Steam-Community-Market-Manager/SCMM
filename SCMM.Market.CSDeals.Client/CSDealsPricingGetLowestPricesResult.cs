using System.Text.Json.Serialization;

namespace SCMM.Market.CSDeals.Client
{
    public class CSDealsPricingGetLowestPricesResult
    {
        [JsonPropertyName("time_updated")]
        public long TimeUpdated { get; set; }

        [JsonPropertyName("appid")]
        public long AppId { get; set; }

        [JsonPropertyName("items")]
        public IEnumerable<CSDealsItemPrice> Items { get; set; }
    }
}
