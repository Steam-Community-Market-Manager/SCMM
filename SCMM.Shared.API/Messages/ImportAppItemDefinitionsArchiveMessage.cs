using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Queue(Name = "Import-App-Item-Definitions-Archive")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 1440 /* 1 day */)]
    public class ImportAppItemDefinitionsArchiveMessage : Message
    {
        public override string Id => $"{AppId}/{ItemDefinitionsDigest}";

        public string AppId { get; set; }

        public string ItemDefinitionsDigest { get; set; }

        public bool ParseChanges { get; set; }
    }
}
