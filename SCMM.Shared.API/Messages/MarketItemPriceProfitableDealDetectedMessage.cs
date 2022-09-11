using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Topic(Name = "Market-Item-Price-Profitable-Deal-Detected")]
    public class MarketItemPriceProfitableDealDetectedMessage : Message
    {

    }
}
