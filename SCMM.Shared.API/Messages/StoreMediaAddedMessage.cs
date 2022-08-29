using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Topic(Name = "Store-Media-Added")]
    public class StoreMediaAddedMessage : IMessage
    {
        public DateTimeOffset? StoreStartedOn { get; set; }

        public string ChannelId { get; set; }

        public string ChannelTitle { get; set; }

        public string VideoId { get; set; }

        public string VideoTitle { get; set; }

        public string VideoThumbnailUrl { get; set; }

        public DateTimeOffset VideoPublishedOn { get; set; }
    }
}
