using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Google.Client;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Job.Server.Jobs.Cron;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Steam.Job.Server.Jobs
{
    public class CheckYouTubeForNewStoreVideosJobConfiguration : CronJobConfiguration
    {
        public ChannelExpression[] Channels { get; set; }

        public class ChannelExpression
        {
            public string ChannelId { get; set; }

            public string Query { get; set; }
        }
    }

    public class CheckYouTubeForNewStoreVideosJobs : CronJobService<CheckYouTubeForNewStoreVideosJobConfiguration>
    {
        private readonly ILogger<CheckYouTubeForNewStoreVideosJobs> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public CheckYouTubeForNewStoreVideosJobs(IConfiguration configuration, ILogger<CheckYouTubeForNewStoreVideosJobs> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<CheckYouTubeForNewStoreVideosJobs, CheckYouTubeForNewStoreVideosJobConfiguration>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var googleClient = scope.ServiceProvider.GetService<GoogleClient>();
            var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

            var steamApps = await db.SteamApps.ToListAsync();
            if (!steamApps.Any())
            {
                return;
            }

            foreach (var app in steamApps)
            {
                var media = new Dictionary<DateTimeOffset, string>();
                var itemStore = db.SteamItemStores
                    .Where(x => x.End == null)
                    .OrderByDescending(x => x.Start)
                    .FirstOrDefault();

                if (itemStore == null)
                {
                    continue;
                }

                foreach (var channel in Configuration.Channels)
                {
                    var videos = await googleClient.ListChannelVideosAsync(channel.ChannelId, GoogleClient.PageMaxResults);
                    var releventVideos = videos
                        .Where(x => Regex.IsMatch(x.Title, channel.Query, RegexOptions.IgnoreCase))
                        .Where(x => x.PublishedAt != null && x.PublishedAt.Value.UtcDateTime >= itemStore.Start.UtcDateTime)
                        .OrderByDescending(x => x.PublishedAt.Value);

                    foreach (var video in releventVideos)
                    {
                        media[video.PublishedAt.Value] = video.Id;
                        /*
                        try
                        {
                            await googleClient.LikeVideoAsync(video.Id);
                            await googleClient.CommentOnVideoAsync(video.ChannelId, video.Id,
                                $"thank you for showcasing this weeks Rust skins, your video has been featured on https://scmm.app/store/{itemStore.Start.ToString(Constants.SCMMStoreIdDateFormat)}"
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to like and comment on new store video (channelId: {video.ChannelId}, videoId: {video.Id})");
                        }
                        */
                    }
                }

                var newMedia = media
                    .Where(x => !itemStore.Media.Contains(x.Value))
                    .OrderBy(x => x.Key)
                    .ToList();

                if (newMedia.Any())
                {
                    itemStore.Media = new PersistableStringCollection(
                        itemStore.Media.Union(newMedia.Select(x => x.Value))
                    );
                    db.SaveChanges();
                }
            }
        }
    }
}
