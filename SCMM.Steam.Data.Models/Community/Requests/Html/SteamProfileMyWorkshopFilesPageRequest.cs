namespace SCMM.Steam.Data.Models.Community.Requests.Html
{
    public class SteamProfileMyWorkshopFilesPageRequest : SteamRequest
    {
        public string SteamId { get; set; }

        public string AppId { get; set; }

        public int Page { get; set; } = 1;

        public int NumberPerPage { get; set; } = 30;

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/profiles/{Uri.EscapeDataString(SteamId)}/myworkshopfiles/?appid={Uri.EscapeDataString(AppId ?? String.Empty)}&p={Page}&numperpage={NumberPerPage}"
        );
    }
}
