namespace SCMM.Shared.Abstractions.Media;

public interface IVideo
{
    public string Id { get; }

    public string ChannelId { get; }

    public string Title { get; }

    public string ChannelTitle { get;}

    public string Description { get; }

    public Uri Thumbnail { get; }

    public DateTimeOffset? PublishedAt { get; }
}
