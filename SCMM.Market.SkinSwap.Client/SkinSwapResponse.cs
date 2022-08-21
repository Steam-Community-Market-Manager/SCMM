using System.Text.Json.Serialization;

namespace SCMM.Market.SkinSwap.Client
{
    public class SkinSwapResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T Data { get; set; }
    }
}
