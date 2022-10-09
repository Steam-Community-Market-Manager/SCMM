namespace SCMM.Steam.Data.Models.Community.Requests.Html
{
    public class SteamBuyItemPageRequest : SteamRequest
    {
        public string AppId { get; set; }

        public string ItemId { get; set; }

        public string Snr { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamStoreUrl}/buyitem/{Uri.EscapeDataString(AppId)}/{Uri.EscapeDataString(ItemId)}/1?snr={Uri.EscapeDataString(Snr ?? String.Empty)}"
        );
    }
}
