using SCMM.Steam.Data.Models.WebApi.Models;
using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Responses.ISteamUser
{
    public class GetPlayerBansJsonResponse
    {
        [JsonPropertyName("players")]
        public List<PlayerBan> Players { get; set; }
    }
}
