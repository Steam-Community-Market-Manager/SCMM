namespace SCMM.Steam.Data.Models.Store.Requests.Html
{
    public class SteamItemStoreDetailPageRequest : SteamRequest
    {
        public string AppId { get; set; }

        public string ItemId { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamStoreUrl}/itemstore/{Uri.EscapeDataString(AppId)}/detail/{Uri.EscapeDataString(ItemId)}"
        );
    }
}
