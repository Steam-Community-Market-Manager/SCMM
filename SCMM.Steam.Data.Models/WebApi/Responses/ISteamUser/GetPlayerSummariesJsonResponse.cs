using SCMM.Steam.Data.Models.WebApi.Models;
using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Responses.ISteamUser
{
    public class GetPlayerSummariesJsonResponse
    {
        [JsonPropertyName("players")]
        public List<PlayerSummary> Players { get; set; }
    }
}
