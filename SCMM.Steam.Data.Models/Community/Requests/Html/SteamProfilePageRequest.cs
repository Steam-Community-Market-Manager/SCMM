namespace SCMM.Steam.Data.Models.Community.Requests.Html
{
    public class SteamProfilePageRequest : SteamRequest
    {
        public string SteamId { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/profiles/{Uri.EscapeDataString(SteamId)}"
        );
    }
}
