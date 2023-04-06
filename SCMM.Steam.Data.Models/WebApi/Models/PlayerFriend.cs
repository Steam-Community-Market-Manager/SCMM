using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Models;

public class PlayerFriend
{
    [JsonPropertyName("steamid")]
    public string SteamId { get; set; }

    [JsonPropertyName("relationship")]
    public bool Relationship { get; set; }

    [JsonPropertyName("friend_since")]
    public ulong FriendSince { get; set; }
}
