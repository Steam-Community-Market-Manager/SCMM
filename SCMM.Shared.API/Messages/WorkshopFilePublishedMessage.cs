using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Topic(Name = "Workshop-File-Published")]
    public class WorkshopFilePublishedMessage : IMessage
    {

    }
}
