using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Topic(Name = "Market-Item-Price-All-Time-High-Reached")]
    public class MarketItemPriceAllTimeHighReachedMessage : IMessage
    {

    }
}
