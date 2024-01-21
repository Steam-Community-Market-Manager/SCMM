namespace SCMM.Web.Data.Models.UI.Profile.Inventory
{
    public class ProfileInventoryItemDescriptionStackDTO
    {
        public string SteamId { get; set; }

        public int Quantity { get; set; }

        public bool TradableAndMarketable { get; set; }

        public DateTimeOffset? TradableAndMarketableAfter { get; set; }
    }
}
