namespace SCMM.Steam.Data.Models.Community.Requests.Html
{
    public class SteamProfileMyWorkshopFilesPageRequest : SteamRequest
    {
        public string SteamId { get; set; }

        public string AppId { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/profiles/{Uri.EscapeDataString(SteamId)}/myworkshopfiles/?appid={Uri.EscapeDataString(AppId)}"
        );
    }
}
