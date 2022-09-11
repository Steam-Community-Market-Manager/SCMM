using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Topic(Name = "App-Item-Definitions-Updated")]
    public class AppItemDefinitionsUpdatedMessage : Message
    {
        public ulong AppId { get; set; }

        public string AppName { get; set; }

        public string AppIconUrl { get; set; }

        public string AppColour { get; set; }

        public string ItemDefinitionsDigest { get; set; }

        public DateTimeOffset? TimeUpdated { get; set; }
    }
}
