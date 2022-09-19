using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Queue(Name = "Import-Profile-Friends")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 1440 /* 1 day */)] 
    public class ImportProfileFriendsMessage : Message
    {
        public override string Id => $"{AppId}/{ProfileId}";

        public ulong AppId { get; set; }

        public string ProfileId { get; set; }

        public bool Force { get; set; }
    }
}
