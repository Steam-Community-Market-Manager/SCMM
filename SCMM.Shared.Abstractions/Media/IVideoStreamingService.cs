namespace SCMM.Shared.Abstractions.Media;

public interface IVideoStreamingService
{
    Task<IEnumerable<IVideo>> ListChannelVideosAsync(string channelId, int? maxResults = null);

    Task<IEnumerable<IVideo>> SearchForVideosAsync(string query, string channelId = null, DateTimeOffset? publishedBefore = null, DateTimeOffset? publishedAfter = null, int? maxResults = null);

    Task CommentOnVideoAsync(string channelId, string videoId, string comment);

    Task LikeVideoAsync(string videoId);
}
