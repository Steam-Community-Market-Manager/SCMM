using Azure.Messaging.ServiceBus;
using SCMM.Azure.ServiceBus.Extensions;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Data.Models.Extensions;

namespace SCMM.Azure.ServiceBus;

internal class ServiceBusJsonMessage<T> : ServiceBusMessage where T : class, IMessage
{
    public ServiceBusJsonMessage(T body) : base(body.ToBinaryAsJson())
    {
        ApplicationProperties[ServiceBusConstants.ApplicationPropertyType] = body.GetType().AssemblyQualifiedName;
        if (!String.IsNullOrEmpty(body.Id))
        {
            MessageId = body.Id.Truncate(128);
        }
    }
}
