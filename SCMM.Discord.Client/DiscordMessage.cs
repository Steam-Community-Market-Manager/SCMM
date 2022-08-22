namespace SCMM.Discord.Client;

public class DiscordMessage
{
    public ulong Id { get; set; }

    public ulong AuthorId { get; set; }

    public string Content { get; set; }

    public IEnumerable<DiscordMessageAttachment> Attachments { get; set; }

    public DateTimeOffset Timestamp { get; set; }
}
