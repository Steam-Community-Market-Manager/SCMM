namespace SCMM.Steam.Data.Models.Community.Requests.Html
{
    public class SteamStoreItemPageRequest : SteamRequest
    {
        public string AppId { get; set; }

        public string ItemId { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamStoreUrl}/itemstore/{Uri.EscapeDataString(AppId)}/detail/{Uri.EscapeDataString(ItemId)}"
        );
    }
}
