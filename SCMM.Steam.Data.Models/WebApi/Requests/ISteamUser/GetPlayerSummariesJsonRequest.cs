namespace SCMM.Steam.Data.Models.WebApi.Requests.ISteamUser
{
    /// <summary>
    /// https://steamapi.xpaw.me/#ISteamUser/GetPlayerSummaries
    /// </summary>
    public class GetPlayerSummariesJsonRequest : SteamRequest
    {
        public string Key { get; set; }

        public string[] SteamIds { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamWebApiUrl}/ISteamUser/GetPlayerSummaries/v2/?key={Uri.EscapeDataString(Key)}&steamids={string.Join(',', SteamIds ?? new string[0])}"
        );
    }
}
