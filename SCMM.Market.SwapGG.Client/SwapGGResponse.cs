using System.Text.Json.Serialization;

namespace SCMM.Market.SwapGG.Client
{
    public class SwapGGResponse<T>
    {
        public const string StatusOk = "OK";

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("result")]
        public T Result { get; set; }
    }
}
