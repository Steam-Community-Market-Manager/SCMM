using Azure.Messaging.ServiceBus;
using SCMM.Shared.Abstractions.Messaging;

namespace SCMM.Azure.ServiceBus;

internal class ServiceBusJsonMessage<T> : ServiceBusMessage where T : class, IMessage
{
    public ServiceBusJsonMessage(T body) : base(BinaryData.FromObjectAsJson(body))
    {
        ApplicationProperties[ServiceBusConstants.ApplicationPropertyType] = typeof(T).AssemblyQualifiedName;
        if (!String.IsNullOrEmpty(body.Id))
        {
            MessageId = body.Id;
        }
    }
}
