namespace SCMM.Steam.Data.Models.Community.Requests.Html
{
    public class SteamWorkshopFileChangeNotesPageRequest : SteamRequest
    {
        public string Id { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/sharedfiles/filedetails/changelog/{Uri.EscapeDataString(Id)}"
        );
    }
}
