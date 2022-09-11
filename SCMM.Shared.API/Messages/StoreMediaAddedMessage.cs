using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Topic(Name = "Store-Media-Added")]
    public class StoreMediaAddedMessage : Message
    {
        public override string Id => $"{AppId}/{ChannelId}/{VideoId}";

        public ulong AppId { get; set; }

        public string AppName { get; set; }

        public string AppIconUrl { get; set; }

        public string AppColour { get; set; }

        public string StoreId { get; set; }

        public string StoreName { get; set; }

        public string ChannelId { get; set; }

        public string ChannelName { get; set; }

        public string VideoId { get; set; }

        public string VideoName { get; set; }

        public string VideoThumbnailUrl { get; set; }

        public DateTimeOffset VideoPublishedOn { get; set; }
    }
}
