using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;
using System.Collections.Generic;

namespace SCMM.Discord.API.Messages
{
    [Queue(Name = "Discord-Prompts")]
    public class DiscordPromptMessage : IMessage
    {
        public string Username { get; set; }

        public DiscordPromptMessageType Type { get; set; }

        public string[] Reactions { get; set; }

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
