using System.Text.Json.Serialization;

namespace SCMM.Market.RustyPot.Client;

public class RustyPotChatMessage : RustyPotProfile
{
    [JsonPropertyName("message")]
    public string Message { get; set; }
}
