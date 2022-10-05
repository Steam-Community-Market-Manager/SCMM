using System;
using System.Text.Json.Serialization;

namespace SCMM.Market.RustyPot.Client;

public class RustyPotProfile
{
    [JsonPropertyName("id")]
    public string ProfileId { get; set; }

    [JsonPropertyName("displayName")]
    public string ProfileName { get; set; }

    [JsonPropertyName("image")]
    public string ProfileAvatarUrl { get; set; }
}
