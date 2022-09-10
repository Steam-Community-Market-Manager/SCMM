namespace SCMM.Discord.Data.Models
{
    public class TextMessage
    {
        public ulong Id { get; set; }

        public ulong AuthorId { get; set; }

        public string Content { get; set; }

        public MessageAttachment[] Attachments { get; set; }

        public DateTimeOffset Timestamp { get; set; }
    }
}
