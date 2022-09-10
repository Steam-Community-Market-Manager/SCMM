using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Discord.API.Messages
{
    [Queue(Name = "Send-Discord-Message")]
    public class SendDiscordMessage : IMessage
    {
        /// <summary>
        /// If set, this message will be sent to an individual user
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// If set, this message will be sent to a guild channel
        /// </summary>
        public ulong GuidId { get; set; }

        /// <summary>
        /// List of regex patterns matching channels ids or names this message should be sent to. 
        /// The message will be sent once, to the first channel found.
        /// </summary>
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
