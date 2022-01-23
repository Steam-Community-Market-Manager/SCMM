using System.Text.Json.Serialization;

namespace SCMM.Market.TradeSkinsFast.Client
{
    public class TradeSkinsFastResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("response")]
        public T Response { get; set; }
    }
}
