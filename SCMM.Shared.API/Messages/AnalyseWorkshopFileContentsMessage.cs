using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Queue(Name = "Analyse-Workshop-File-Contents")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 1440 /* 1 day */)]
    public class AnalyseWorkshopFileContentsMessage : Message
    {
        public override string Id => $"{BlobName}";

        public string BlobName { get; set; }

        public bool Force { get; set; }
    }
}
