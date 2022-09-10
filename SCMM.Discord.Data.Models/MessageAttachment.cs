namespace SCMM.Discord.Data.Models
{
    public class MessageAttachment
    {
        public ulong Id { get; set; }

        public string Url { get; set; }

        public string FileName { get; set; }

        public string ContentType { get; set; }

        public string Description { get; set; }
    }
}
