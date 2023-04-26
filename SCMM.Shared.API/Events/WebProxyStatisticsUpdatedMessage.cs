using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;

namespace SCMM.Shared.API.Events
{
    [Topic(Name = "Web-Proxy-Statistics-Updated")]
    public class WebProxyStatisticsUpdatedMessage : Message
    {
    }
}
