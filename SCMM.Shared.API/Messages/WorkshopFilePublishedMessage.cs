using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Topic(Name = "Workshop-File-Published")]
    public class WorkshopFilePublishedMessage : IMessage
    {
        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public ulong? CreatorId { get; set; }

        public string CreatorName { get; set; }

        public string CreatorAvatarUrl { get; set; }

        public string ItemType { get; set; }

        public string ItemShortName { get; set; }

        public string ItemCollection { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string PreviewUrl { get; set; }

        public DateTimeOffset TimeCreated { get; set; }
    }
}
