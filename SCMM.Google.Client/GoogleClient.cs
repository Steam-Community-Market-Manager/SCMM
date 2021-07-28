using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SCMM.Google.Client
{
    public class GoogleClient : IDisposable
    {
        public const int PageMaxResults = 50;

        private const string ContentDetails = "contentDetails";
        private const string Snippet = "snippet";

        private readonly ILogger<GoogleClient> _logger;
        private readonly GoogleConfiguration _configuration;
        private readonly YouTubeService _service;
        private bool disposedValue;

        public GoogleClient(ILogger<GoogleClient> logger, GoogleConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _service = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _configuration.ApiKey,
                ApplicationName = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyTitleAttribute>()?.Title
            });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _service.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <remarks>
        /// A call to this method has a quota cost of 1 unit + an additional 1 unit for every page of videos returned (see PageMaxResults).
        /// </remarks>
        public async Task<IEnumerable<YouTubeVideo>> ListChannelVideosAsync(string channelId, int? maxResults = PageMaxResults)
        {
            var channelDetailsRequest = _service.Channels.List(ContentDetails);
            channelDetailsRequest.Id = channelId;

            var channelDetailsResponse = await channelDetailsRequest.ExecuteAsync(); // Quota cost: 1 unit
            var uploadPlaylistId = channelDetailsResponse?.Items.FirstOrDefault()?.ContentDetails?.RelatedPlaylists?.Uploads;
            if (String.IsNullOrEmpty(uploadPlaylistId))
            {
                throw new Exception($"Unable to locate upload playlist for channel: {channelId}");
            }

            var videos = new List<YouTubeVideo>();
            var nextPageToken = (string)null;
            do
            {
                var videosListRequest = _service.PlaylistItems.List(Snippet); // Quota cost: 1 unit
                videosListRequest.PlaylistId = uploadPlaylistId;
                videosListRequest.MaxResults = PageMaxResults;
                videosListRequest.PageToken = nextPageToken;

                var videosListResponse = await videosListRequest.ExecuteAsync();
                foreach (var item in videosListResponse.Items)
                {
                    var videoId = item.Snippet?.ResourceId?.VideoId;
                    if (!String.IsNullOrEmpty(videoId) && !videos.Any(x => x.Id == videoId))
                    {
                        videos.Add(new YouTubeVideo()
                        {
                            Id = videoId,
                            ChannelId = item.Snippet.ChannelId,
                            Title = item.Snippet.Title,
                            ChannelTitle = item.Snippet.ChannelTitle,
                            Description = item.Snippet.Description,
                            Thumbnail = new Uri(item.Snippet.Thumbnails.Default__.Url),
                            PublishedAt = new DateTimeOffset(item.Snippet.PublishedAt.Value, TimeZoneInfo.Local.GetUtcOffset(item.Snippet.PublishedAt.Value))
                        });
                    }
                }

                nextPageToken = videosListResponse?.NextPageToken;
            } while (!String.IsNullOrEmpty(nextPageToken) && (maxResults == null || videos.Count < maxResults));

            return (maxResults > 0 && videos.Count > maxResults)
                ? videos.Take(maxResults.Value)
                : videos;
        }

        /// <remarks>
        /// A call to this method has a quota cost of 100 units for every page of videos returned (see PageMaxResults).
        /// </remarks>
        public async Task<IEnumerable<YouTubeVideo>> SearchForVideosAsync(string query, string channelId = null, DateTime? publishedBefore = null, DateTime? publishedAfter = null, int? maxResults = PageMaxResults)
        {
            var videos = new List<YouTubeVideo>();
            var nextPageToken = (string)null;
            do
            {
                var videosListRequest = _service.Search.List(Snippet);
                videosListRequest.Q = query;
                videosListRequest.ChannelId = channelId;
                videosListRequest.PublishedBefore = publishedBefore;
                videosListRequest.PublishedAfter = publishedAfter;
                videosListRequest.Order = SearchResource.ListRequest.OrderEnum.Relevance;
                videosListRequest.MaxResults = PageMaxResults;
                videosListRequest.PageToken = nextPageToken;

                var videosListResponse = await videosListRequest.ExecuteAsync(); // Quota cost: 100 units
                foreach (var item in videosListResponse.Items)
                {
                    var videoId = item.Id.VideoId;
                    if (!String.IsNullOrEmpty(videoId) && !videos.Any(x => x.Id == videoId))
                    {
                        videos.Add(new YouTubeVideo()
                        {
                            Id = videoId,
                            ChannelId = item.Snippet.ChannelId,
                            Title = item.Snippet.Title,
                            ChannelTitle = item.Snippet.ChannelTitle,
                            Description = item.Snippet.Description,
                            Thumbnail = new Uri(item.Snippet.Thumbnails.Default__.Url),
                            PublishedAt = new DateTimeOffset(item.Snippet.PublishedAt.Value, TimeZoneInfo.Local.GetUtcOffset(item.Snippet.PublishedAt.Value))
                        });
                    }
                }

                nextPageToken = videosListResponse?.NextPageToken;
            } while (!String.IsNullOrEmpty(nextPageToken) && (maxResults == null || videos.Count < maxResults));

            return (maxResults > 0 && videos.Count > maxResults)
                ? videos.Take(maxResults.Value)
                : videos;
        }

        /// <remarks>
        /// A call to this method has a quota cost of 50 units.
        /// </remarks>
        public async Task CommentOnVideoAsync(string channelId, string videoId, string comment)
        {
            var commentThread = new CommentThread()
            {
                Snippet = new CommentThreadSnippet()
                {
                    ChannelId = channelId,
                    VideoId = videoId,
                    TopLevelComment = new Comment()
                    {
                        Snippet = new CommentSnippet()
                        {
                            TextOriginal = comment
                        }
                    }
                }
            };

            var request = _service.CommentThreads.Insert(commentThread, Snippet); // Quota cost: 50 units
            await request.ExecuteAsync();
        }

        /// <remarks>
        /// A call to this method has a quota cost of 50 units.
        /// </remarks>
        public async Task LikeVideoAsync(string videoId)
        {
            var request = _service.Videos.Rate(videoId, VideosResource.RateRequest.RatingEnum.Like); // Quota cost: 50 units
            await request.ExecuteAsync();
        }
    }
}
