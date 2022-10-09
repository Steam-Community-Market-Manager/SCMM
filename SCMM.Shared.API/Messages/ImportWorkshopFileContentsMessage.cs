using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Queue(Name = "Import-Workshop-File-Contents")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 1440 /* 1 day */)]
    public class ImportWorkshopFileContentsMessage : Message
    {
        public override string Id => $"{AppId}/{PublishedFileId}";

        public ulong AppId { get; set; }

        public ulong PublishedFileId { get; set; }

        public bool Force { get; set; }
    }
}
