using System.Text.Json.Serialization;

namespace SCMM.Market.ShadowPay.Client
{
    public class ShadowPayItemsResponse
    {
        [JsonPropertyName("data")]
        public IEnumerable<ShadowPayItem> Data { get; set; }
    }
}
