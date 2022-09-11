using Azure.Messaging.ServiceBus;

namespace SCMM.Azure.ServiceBus;

internal class ServiceBusJsonMessage<T> : ServiceBusMessage where T : class, IMessage
{
    public ServiceBusJsonMessage(T body) : base(BinaryData.FromObjectAsJson(body))
    {
        ApplicationProperties[IMessage.ApplicationPropertyType] = typeof(T).AssemblyQualifiedName;
        if (!String.IsNullOrEmpty(body.Id))
        {
            MessageId = body.Id;
        }
    }
}
