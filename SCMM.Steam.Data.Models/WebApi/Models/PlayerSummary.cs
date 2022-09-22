using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Models;

public class PlayerSummary
{
    [JsonPropertyName("steamId")]
    public string SteamId { get; set; }

    [JsonPropertyName("communityvisibilitystate")]
    public uint CommunityVisibilityState { get; set; }

    [JsonPropertyName("profilestate")]
    public uint ProfileState { get; set; }

    [JsonPropertyName("personaname")]
    public string PersonaName { get; set; }

    [JsonPropertyName("profileurl")]
    public string ProfileUrl { get; set; }

    [JsonPropertyName("avatar")]
    public string AvatarUrl { get; set; }

    [JsonPropertyName("avatarmedium")]
    public string AvatarMediumUrl { get; set; }

    [JsonPropertyName("avatarfull")]
    public string AvatarFullUrl { get; set; }

    [JsonPropertyName("avatarhash")]
    public string AvatarHash { get; set; }

    [JsonPropertyName("lastlogoff")]
    public ulong LastLogoff { get; set; }

    [JsonPropertyName("personastate")]
    public uint PersonaState { get; set; }

    [JsonPropertyName("primaryclanid")]
    public string PrimaryClanId { get; set; }

    [JsonPropertyName("timecreated")]
    public ulong TimeCreated { get; set; }

    [JsonPropertyName("personastateflags")]
    public uint PersonaStateFlags { get; set; }

    [JsonPropertyName("loccountrycode")]
    public string LocCountryCode { get; set; }
}
