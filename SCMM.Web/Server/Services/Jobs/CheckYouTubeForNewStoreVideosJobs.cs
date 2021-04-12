using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Google.Client;
using SCMM.Steam.Client;
using SCMM.Steam.Shared.Community.Requests.Html;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Types;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
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
            using (var scope = _scopeFactory.CreateScope())
            {
                var googleClient = scope.ServiceProvider.GetService<GoogleClient>();
                var db = scope.ServiceProvider.GetRequiredService<ScmmDbContext>();

                var steamApps = await db.SteamApps.ToListAsync();
                if (!steamApps.Any())
                {
                    return;
                }

                foreach (var app in steamApps)
                {
                    var media = new Dictionary<DateTime, string>();
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
                        var videos = await googleClient.SearchVideos(
                            query: channel.Query,
                            channelId: channel.ChannelId,
                            publishedBefore: itemStore.End?.UtcDateTime,
                            publishedAfter: itemStore.Start.UtcDateTime
                        );
                        if (videos?.Any() == true)
                        {
                            foreach (var video in videos.Where(x => x.PublishedAt != null))
                            {
                                if (video.Title.Contains(channel.Query.Trim('\"'), StringComparison.InvariantCultureIgnoreCase))
                                {
                                    media[video.PublishedAt.Value] = video.Id;
                                }
                            }
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
}
