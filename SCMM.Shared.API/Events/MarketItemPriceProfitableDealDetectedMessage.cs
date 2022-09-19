using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Events
{
    [Topic(Name = "Market-Item-Price-Profitable-Deal-Detected")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 1440 /* 1 day */)]
    public class MarketItemPriceProfitableDealDetectedMessage : Message
    {

    }
}
