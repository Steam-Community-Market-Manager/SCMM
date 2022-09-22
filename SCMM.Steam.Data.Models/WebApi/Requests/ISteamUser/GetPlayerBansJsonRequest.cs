namespace SCMM.Steam.Data.Models.WebApi.Requests.ISteamUser
{
    /// <summary>
    /// https://steamapi.xpaw.me/#ISteamUser/GetPlayerBans
    /// </summary>
    public class GetPlayerBansJsonRequest : SteamRequest
    {
        public string Key { get; set; }

        public string[] SteamIds { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamWebApiUrl}/ISteamUser/GetPlayerBans/v1/?key={Uri.EscapeDataString(Key)}&steamids={string.Join(',', SteamIds ?? new string[0])}"
        );
    }
}
