using SCMM.Shared.Data.Store;

namespace SCMM.Steam.Data.Store
{
    public class SteamMarketItemOrderSummary : Entity
    {
        public DateTimeOffset Timestamp { get; set; }

        public int BuyCount { get; set; }

        public long BuyCumulativePrice { get; set; }

        public long BuyHighestPrice { get; set; }

        public int SellCount { get; set; }

        public long SellCumulativePrice { get; set; }

        public long SellLowestPrice { get; set; }

        public Guid ItemId { get; set; }

        public SteamMarketItem Item { get; set; }
    }
}
