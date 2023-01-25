using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;

namespace SCMM.Shared.API.Events
{
    [Topic(Name = "Market-Item-Price-All-Time-High-Reached")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 1440 /* 1 day */)]
    public class MarketItemPriceAllTimeHighReachedMessage : Message
    {
        public override string Id => $"{AppId}/{ItemId}";

        public ulong AppId { get; set; }

        public string AppName { get; set; }

        public ulong ItemId { get; set; }

        public string ItemName { get; set; }

        public string ItemType { get; set; }

        public string ItemShortName { get; set; }

        public string ItemIconUrl { get; set; }

        public string Currency { get; set; }

        public long AllTimeHighestValue { get; set; }

        public string AllTimeHighestValueDescription { get; set; }

        public DateTimeOffset AllTimeHighestValueOn { get; set; }
    }
}
