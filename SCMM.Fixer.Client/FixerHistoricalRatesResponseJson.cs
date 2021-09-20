using System.Text.Json.Serialization;

namespace SCMM.Fixer.Client
{
    public class FixerHistoricalRatesResponseJson
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("historical")]
        public bool Historical { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("base")]
        public string Base { get; set; }

        [JsonPropertyName("rates")]
        public IDictionary<string, decimal> Rates { get; set; }
    }
}
