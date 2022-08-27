using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Topic(Name = "Store-Added")]
    public class StoreAddedMessage : IMessage
    {

    }
}
