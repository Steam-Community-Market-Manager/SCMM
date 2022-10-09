using System.Text.Json.Serialization;

namespace SCMM.Market.RustyPot.Client;

public class RustyPotCoinflip
{
    [JsonPropertyName("creator")]
    public RustyPotProfile Creator { get; set; }

    [JsonPropertyName("opponent")]
    public RustyPotProfile Opponent { get; set; }

    [JsonPropertyName("winner")]
    public RustyPotProfile Winner { get; set; }

    [JsonPropertyName("bot")]
    public string BotProfileId { get; set; }
}
