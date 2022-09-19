using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Queue(Name = "Import-Profile-Inventory")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 1440 /* 1 day */)] 
    public class ImportProfileInventoryMessage : Message
    {
        public override string Id => $"{ProfileId}/{(AppIds?.Length > 0 ? String.Join('+', AppIds) : "*")}";

        public string ProfileId { get; set; }

        public string[] AppIds { get; set; }

        public bool Force { get; set; }
    }
}
