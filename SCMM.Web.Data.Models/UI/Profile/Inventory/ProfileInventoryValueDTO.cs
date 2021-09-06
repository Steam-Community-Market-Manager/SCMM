namespace SCMM.Web.Data.Models.UI.Profile.Inventory
{
    public class ProfileInventoryValueDTO
    {
        public string SteamId { get; set; }

        public string Name { get; set; }

        public string AvatarUrl { get; set; }

        public string InventoryMosaicUrl { get; set; }

        public int Items { get; set; }

        public long? Invested { get; set; }

        public long? InvestmentGains { get; set; }

        public long? InvestmentLosses { get; set; }

        public long BuyNowValue { get; set; }

        public long MarketValue { get; set; }

        public long MarketMovementValue { get; set; }

        public DateTimeOffset MarketMovementTime { get; set; }
    }
}
