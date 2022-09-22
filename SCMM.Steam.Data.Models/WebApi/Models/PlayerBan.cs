using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Models;

public class PlayerBan
{
    [JsonPropertyName("SteamId")]
    public string SteamId { get; set; }

    [JsonPropertyName("CommunityBanned")]
    public bool CommunityBanned { get; set; }

    [JsonPropertyName("VACBanned")]
    public bool VACBanned { get; set; }

    [JsonPropertyName("NumberOfVACBans")]
    public uint NumberOfVACBans { get; set; }

    [JsonPropertyName("DaysSinceLastBan")]
    public uint DaysSinceLastBan { get; set; }

    [JsonPropertyName("NumberOfGameBans")]
    public uint NumberOfGameBans { get; set; }

    [JsonPropertyName("EconomyBan")]
    public string EconomyBan { get; set; }
}
