using SCMM.Steam.Data.Models.WebApi.Models;
using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Responses.ISteamUser
{
    public class GetFriendListJsonResponse
    {
        [JsonPropertyName("friends")]
        public List<PlayerFriend> Friends { get; set; }
        
        public class Result
        {
            [JsonPropertyName("friendslist")]
            public GetFriendListJsonResponse FriendsList { get; set; }
        }
    }
}
