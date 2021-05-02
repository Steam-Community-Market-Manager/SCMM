using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Logging;
using SCMM.Google.Data.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace SCMM.Google.Client
{
    public class GoogleClient : IDisposable
    {
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

        public async Task<IEnumerable<YouTubeVideo>> SearchVideosAsync(string query, string channelId = null, DateTime? publishedBefore = null, DateTime? publishedAfter = null, int maxResults = 30)
        {
            var videos = new List<YouTubeVideo>();
            var request = _service.Search.List("snippet");
            request.Q = query;
            request.ChannelId = channelId;
            request.PublishedBefore = publishedBefore;
            request.PublishedAfter = publishedAfter;
            request.Order = SearchResource.ListRequest.OrderEnum.Relevance;
            request.MaxResults = Math.Max(1, maxResults);

            var response = await request.ExecuteAsync();
            foreach (var item in response.Items)
            {
                if (!String.IsNullOrEmpty(item.Id.VideoId))
                {
                    videos.Add(new YouTubeVideo()
                    {
                        Id = item.Id.VideoId,
                        ChannelId = item.Snippet.ChannelId,
                        Title = item.Snippet.Title,
                        ChannelTitle = item.Snippet.ChannelTitle,
                        Description = item.Snippet.ChannelId,
                        Thumbnail = new Uri(item.Snippet.Thumbnails.Default__.Url),
                        PublishedAt = item.Snippet.PublishedAt
                    });
                }
            }

            return videos;
        }
    }
}
