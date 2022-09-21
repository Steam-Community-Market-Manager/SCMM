using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Events
{
    [Topic(Name = "Workshop-File-Published")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 60)]
    public class WorkshopFilePublishedMessage : Message
    {
        public override string Id => $"{AppId}/{ItemId}";

        public ulong AppId { get; set; }

        public string AppName { get; set; }

        public string AppIconUrl { get; set; }

        public string AppColour { get; set; }

        public ulong CreatorId { get; set; }

        public string CreatorName { get; set; }

        public string CreatorAvatarUrl { get; set; }

        public ulong ItemId { get; set; }

        public string ItemType { get; set; }

        public string ItemShortName { get; set; }

        public string ItemName { get; set; }

        public string ItemDescription { get; set; }

        public string ItemCollection { get; set; }

        public string ItemIconUrl { get; set; }

        public string ItemImageUrl { get; set; }
    }
}
