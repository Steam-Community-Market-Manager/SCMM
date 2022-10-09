using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;

namespace SCMM.Shared.API.Events
{
    [Topic(Name = "Market-Item-Price-Profitable-Deal-Detected")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 1440 /* 1 day */)]
    public class MarketItemPriceProfitableDealDetectedMessage : Message
    {

    }
}
