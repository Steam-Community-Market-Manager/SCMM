using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;
using System.Collections.Generic;

namespace SCMM.Discord.API.Messages
{
    [Queue(Name = "Discord-Notifications")]
    public class DiscordNotificationMessage : IMessage
    {
        public string GuildPattern { get; set; }

        public string ChannelPattern { get; set; }

        public string Message { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public IDictionary<string, string> Fields { get; set; }

        public bool FieldsInline { get; set; }

        public string Url { get; set; }

        public string ThumbnailUrl { get; set; }

        public string ImageUrl { get; set; }

        public string Colour { get; set; }
    }
}
