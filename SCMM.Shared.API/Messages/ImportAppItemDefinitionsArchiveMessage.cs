using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Queue(Name = "Import-App-Item-Definitions-Archive")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 1440 /* 1 day */)]
    public class ImportAppItemDefinitionsArchiveMessage : Message
    {
        public override string Id => $"{AppId}/{ItemDefinitionsDigest}";

        public string AppId { get; set; }

        public string ItemDefinitionsDigest { get; set; }
    }
}
