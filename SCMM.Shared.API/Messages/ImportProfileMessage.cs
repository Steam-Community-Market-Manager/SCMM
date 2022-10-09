using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Queue(Name = "Import-Profile")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 1440 /* 1 day */)]
    public class ImportProfileMessage : Message
    {
        public override string Id => $"{ProfileId}";

        public string ProfileId { get; set; }
    }
}
