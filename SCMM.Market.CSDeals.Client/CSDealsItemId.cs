using System.Text.Json.Serialization;

namespace SCMM.Market.CSDeals.Client
{
    public class CSDealsItemId
    {
        [JsonPropertyName("f")]
        public string SteamId { get; set; }

        [JsonPropertyName("u")]
        public uint Context { get; set; }

        [JsonPropertyName("t")]
        public ulong ItemId { get; set; }
    }
}
