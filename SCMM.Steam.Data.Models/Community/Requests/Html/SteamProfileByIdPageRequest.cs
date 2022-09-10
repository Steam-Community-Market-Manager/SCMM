namespace SCMM.Steam.Data.Models.Community.Requests.Html
{
    public class SteamProfileByIdPageRequest : SteamRequest
    {
        public string ProfileId { get; set; }

        public bool Xml { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/id/{Uri.EscapeDataString(ProfileId)}/?xml={(Xml ? "1" : "0")}"
        );
    }
}
