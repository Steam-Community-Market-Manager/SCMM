namespace SCMM.Steam.Data.Models.Community.Requests.Html
{
    public class SteamMarketMultisellPageRequest : SteamRequest
    {
        public string AppId { get; set; }

        public string[] MarketHashNames { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/market/multisell?appid={Uri.EscapeDataString(AppId ?? string.Empty)}&contextid=2&items[]={String.Join("&items[]=", MarketHashNames.Select(x => Uri.EscapeDataString(x ?? string.Empty)))}"
        );
    }
}
