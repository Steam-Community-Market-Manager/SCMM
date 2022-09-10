using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Topic(Name = "Market-Item-Manipulation-Detected")]
    public class MarketItemManipulationDetectedMessage : IMessage
    {

    }
}
