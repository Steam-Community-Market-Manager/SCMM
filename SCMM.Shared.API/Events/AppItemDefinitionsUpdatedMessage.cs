using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;

namespace SCMM.Shared.API.Events
{
    [Topic(Name = "App-Item-Definitions-Updated")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 10080 /* 7 days */)]
    public class AppItemDefinitionsUpdatedMessage : Message
    {
        public override string Id => $"{AppId}/{ItemDefinitionsDigest}";

        public ulong AppId { get; set; }

        public string AppName { get; set; }

        public string AppIconUrl { get; set; }

        public string AppColour { get; set; }

        public string ItemDefinitionsDigest { get; set; }

        public DateTimeOffset? TimeUpdated { get; set; }
    }
}
