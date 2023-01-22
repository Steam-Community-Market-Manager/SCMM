namespace SCMM.Steam.Data.Models.Store.Requests.Html
{
    public class SteamBuyItemPageRequest : SteamRequest
    {
        public string AppId { get; set; }

        public string ItemDefinitionId { get; set; }

        public int Quantity { get; set; } = 1;

        public override Uri Uri => new Uri(
            $"{Constants.SteamStoreUrl}/buyitem/{Uri.EscapeDataString(AppId)}/{Uri.EscapeDataString(ItemDefinitionId)}/{Quantity}"
        );
    }
}
