namespace SCMM.Google.Client
{
    public class YouTubeVideo
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
