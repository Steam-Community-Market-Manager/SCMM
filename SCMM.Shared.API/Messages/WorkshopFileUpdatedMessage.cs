using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Topic(Name = "Workshop-File-Updated")]
    public class WorkshopFileUpdatedMessage : IMessage
    {

    }
}
