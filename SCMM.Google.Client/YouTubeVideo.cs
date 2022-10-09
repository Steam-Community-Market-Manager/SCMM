using SCMM.Shared.Abstractions.Media;

namespace SCMM.Google.Client
{
    public class YouTubeVideo : IVideo
    {
        public string Id { get; set; }

        public string ChannelId { get; set; }

        public string Title { get; set; }

        public string ChannelTitle { get; set; }

        public string Description { get; set; }

        public Uri Thumbnail { get; set; }

        public DateTimeOffset? PublishedAt { get; set; }
    }
}
