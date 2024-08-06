using System.Text.Json.Serialization;

namespace SCMM.Market.iTradegg.Client
{
    public class iTradeggResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T Data { get; set; }
    }
}
