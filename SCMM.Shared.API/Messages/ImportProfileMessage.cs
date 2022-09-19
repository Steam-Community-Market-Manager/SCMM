using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

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
