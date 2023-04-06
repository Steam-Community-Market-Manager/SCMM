namespace SCMM.Steam.Data.Models.WebApi.Requests.ISteamUser
{
    /// <summary>
    /// https://steamapi.xpaw.me/#ISteamUser/GetFriendList
    /// </summary>
    public class GetFriendListJsonRequest : SteamRequest
    {
        public string Key { get; set; }

        public string SteamId { get; set; }

        public string Relationship { get; set; } = "friend";

        public override Uri Uri => new Uri(
            $"{Constants.SteamWebApiUrl}/ISteamUser/GetFriendList/v1/?key={Uri.EscapeDataString(Key)}&steamid={Uri.EscapeDataString(SteamId)}&relationship={Uri.EscapeDataString(Relationship ?? String.Empty)}"
        );
    }
}
