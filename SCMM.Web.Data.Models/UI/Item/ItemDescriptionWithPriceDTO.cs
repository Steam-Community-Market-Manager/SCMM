using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemDescriptionWithPriceDTO : ItemDescriptionDTO, ICanBeOwned, ICanBePurchased, ICanBeInteractedWith
    {
        public long? OriginalPrice { get; set; }

        public MarketType? BuyNowFrom { get; set; }

        public long? BuyNowPrice { get; set; }

        public string BuyNowUrl { get; set; }

        public long? Subscriptions { get; set; }

        public long? SupplyTotalEstimated { get; set; }

        public long? Supply { get; set; }

        public long? Demand { get; set; }

        public long? AllTimeLowestValue { get; set; }

        public long? AllTimeHighestValue { get; set; }

        public ItemInteractionDTO[] Actions { get; set; }

        public long? PriceMovement => (BuyNowPrice - OriginalPrice) ?? 0;

        public float DistanceToAllTimeLowestValue => (BuyNowPrice > 0 && AllTimeLowestValue > 0) ? ((float)BuyNowPrice / (float)AllTimeLowestValue) : 0;

        public float DistanceToAllTimeHighestValue => (BuyNowPrice > 0 && AllTimeHighestValue > 0) ? ((float)BuyNowPrice / (float)AllTimeHighestValue) : 0;
    }
}
