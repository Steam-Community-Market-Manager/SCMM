using SCMM.Azure.ServiceBus;

namespace SCMM.Discord.API.Messages
{
    public class DiscordGetMessagesReply : IMessage
    {
        public Message[] Messages { get; set; }

        public class Message
        {
            public ulong Id { get; set; }

            public ulong AuthorId { get; set; }

            public string Content { get; set; }

            public MessageAttachment[] Attachments { get; set; }

            public DateTimeOffset Timestamp { get; set; }
        }

        public class MessageAttachment
        {
            public ulong Id { get; set; }

            public string Url { get; set; }

            public string FileName { get; set; }

            public string ContentType { get; set; }

            public string Description { get; set; }
        }
    }
}
