using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Topic(Name = "App-Item-Definitions-Updated")]
    public class AppItemDefinitionsUpdatedMessage : IMessage
    {
        public string AppId { get; set; }

        public string AppName { get; set; }

        public string AppIconUrl { get; set; }

        public string AppPrimaryColour { get; set; }

        public string Digest { get; set; }

        public DateTimeOffset? TimeUpdated { get; set; }
    }
}
