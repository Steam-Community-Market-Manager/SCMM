using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;

namespace SCMM.Discord.API.Messages
{
    [Queue(Name = "Prompt-Discord-Message")]
    public class PromptDiscordMessage : Message
    {
        public enum PromptType
        {
            Reply = 0,
            React
        };

        public PromptType Type { get; set; }

        public string[] Reactions { get; set; }

        public string Username { get; set; }

        public string Message { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public IDictionary<string, string> Fields { get; set; }

        public bool FieldsInline { get; set; }

        public string Url { get; set; }

        public string ThumbnailUrl { get; set; }

        public string ImageUrl { get; set; }

        public uint Colour { get; set; }

        public class Reply : Message
        {
            public string Content { get; set; }
        }
    }
}
