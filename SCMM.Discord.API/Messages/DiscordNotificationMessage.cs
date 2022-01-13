using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Discord.API.Messages
{
    [Queue(Name = "Discord-Notifications")]
    public class DiscordNotificationMessage : IMessage
    {
        public string Username { get; set; }

        public ulong GuidId { get; set; }

        public string[] ChannelPatterns { get; set; }

        public string Message { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public IDictionary<string, string> Fields { get; set; }

        public bool FieldsInline { get; set; }

        public string Url { get; set; }

        public string ThumbnailUrl { get; set; }

        public string ImageUrl { get; set; }

        public uint Colour { get; set; }
    }
}
