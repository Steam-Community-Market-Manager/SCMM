using System.Text.Json.Serialization;

namespace SCMM.Market.ShadowPay.Client
{
    public class ShadowPayItemPricesResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("data")]
        public IEnumerable<ShadowPayItem> Data { get; set; }
    }
}
