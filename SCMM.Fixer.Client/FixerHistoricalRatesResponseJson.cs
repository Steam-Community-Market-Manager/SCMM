using Newtonsoft.Json;

namespace SCMM.Fixer.Client
{
    public class FixerHistoricalRatesResponseJson
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("historical")]
        public bool Historical { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("base")]
        public string Base { get; set; }

        [JsonProperty("rates")]
        public IDictionary<string, decimal> Rates { get; set; }
    }
}
