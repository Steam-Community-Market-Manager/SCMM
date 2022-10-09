using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;

namespace SCMM.Shared.API.Events
{
    [Topic(Name = "Market-Item-Price-All-Time-High-Reached")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 1440 /* 1 day */)]
    public class MarketItemPriceAllTimeHighReachedMessage : Message
    {

    }
}
