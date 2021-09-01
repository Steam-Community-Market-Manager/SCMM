namespace SCMM.Steam.Data.Models.Community.Requests.Html
{
    public class SteamWorkshopFileCommentsPageRequest : SteamRequest
    {
        public string Id { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/sharedfiles/filedetails/comments/{Uri.EscapeDataString(Id)}"
        );
    }
}
