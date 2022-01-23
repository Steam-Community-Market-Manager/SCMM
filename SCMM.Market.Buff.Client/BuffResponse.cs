using System.Text.Json.Serialization;

namespace SCMM.Market.Buff.Client
{
    public class BuffResponse<T>
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("data")]
        public T Data { get; set; }

        [JsonPropertyName("msg")]
        public string Message { get; set; }
    }
}
