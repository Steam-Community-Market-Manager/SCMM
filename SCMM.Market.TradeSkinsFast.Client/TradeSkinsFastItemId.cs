using System.Text.Json.Serialization;

namespace SCMM.Market.TradeSkinsFast.Client
{
    public class TradeSkinsFastItemId
    {
        [JsonPropertyName("f")]
        public string SteamId { get; set; }

        [JsonPropertyName("u")]
        public uint Context { get; set; }

        [JsonPropertyName("t")]
        public ulong ItemId { get; set; }
    }
}
