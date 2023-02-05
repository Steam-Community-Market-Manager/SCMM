using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;
using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Shared.API.Events
{
    [Topic(Name = "Market-Item-Price-Profitable-Buy-Deal-Detected")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 360 /* 6 hours */)]
    public class MarketItemPriceProfitableBuyDealDetectedMessage : Message
    {
        public override string Id => $"{AppId}/{DescriptionId}/{BuyNowFrom}";

        public Guid AppId { get; set; }

        public Guid DescriptionId { get; set; }

        public Guid CurrencyId { get; set; }

        public long SellOrderLowestPrice { get; set; }

        public MarketType BuyNowFrom { get; set; }

        public long BuyNowPrice { get; set; }

        public long BuyNowFee { get; set; }
    }
}
