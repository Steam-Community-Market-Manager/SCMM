namespace SCMM.Steam.Data.Models.Community.Requests.Json
{
    public class SteamMarketAppFiltersJsonRequest : SteamRequest
    {
        public string AppId { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/market/appfilters/{Uri.EscapeDataString(AppId)}"
        );
    }
}
